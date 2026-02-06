//! TCP connection management for debug communication.

use std::sync::Arc;
use tokio::io::{AsyncReadExt, AsyncWriteExt};
use tokio::net::TcpStream;
use tokio::sync::{Mutex, RwLock};
use tauri::{AppHandle, Emitter};

use super::protocol::{decode_messages, encode_message};

/// Connection state
#[derive(Clone, Default)]
pub struct ConnectionState {
    writer: Arc<Mutex<Option<tokio::net::tcp::OwnedWriteHalf>>>,
    is_connected: Arc<RwLock<bool>>,
}

impl ConnectionState {
    pub fn new() -> Self {
        Self {
            writer: Arc::new(Mutex::new(None)),
            is_connected: Arc::new(RwLock::new(false)),
        }
    }

    pub async fn is_connected(&self) -> bool {
        *self.is_connected.read().await
    }

    /// Connect to the game runtime
    pub async fn connect(
        &self,
        app_handle: AppHandle,
        ip: String,
        port: u16,
    ) -> Result<(), String> {
        // Check if already connected
        if self.is_connected().await {
            return Err("Already connected".to_string());
        }

        // Establish TCP connection
        let addr = format!("{}:{}", ip, port);
        let stream = TcpStream::connect(&addr)
            .await
            .map_err(|e| format!("Connection failed: {}", e))?;

        // Disable Nagle's algorithm to send small messages immediately
        stream.set_nodelay(true).map_err(|e| format!("Failed to set nodelay: {}", e))?;

        let (reader, writer) = stream.into_split();

        // Store the writer
        {
            let mut writer_guard = self.writer.lock().await;
            *writer_guard = Some(writer);
        }
        
        *self.is_connected.write().await = true;

        // Emit connection event
        let _ = app_handle.emit("debug:connection", serde_json::json!({ "connected": true }));

        // Start the receive loop
        self.start_receive_loop(app_handle, reader).await;

        Ok(())
    }

    /// Start the background receive loop
    async fn start_receive_loop(&self, app_handle: AppHandle, mut reader: tokio::net::tcp::OwnedReadHalf) {
        let is_connected_arc = self.is_connected.clone();
        let writer_arc = self.writer.clone();

        tokio::spawn(async move {
            let mut buffer = vec![0u8; 16384 * 10]; // Same buffer size as old editor
            let mut pending_data: Vec<u8> = Vec::new();

            loop {
                // Check connection status
                if !*is_connected_arc.read().await {
                    break;
                }

                // Try to read data
                match reader.read(&mut buffer).await {
                    Ok(0) => {
                        // Connection closed
                        break;
                    }
                    Ok(n) => {
                        // Append received data to pending buffer
                        pending_data.extend_from_slice(&buffer[..n]);

                        // Decode messages
                        let (messages, remaining) = decode_messages(&pending_data);
                        pending_data = remaining;

                        // Emit messages to frontend
                        for msg in messages {
                            let _ = app_handle.emit("debug:message", &msg);
                        }
                    }
                    Err(_) => {
                        break;
                    }
                }
            }

            // Connection ended - clean up
            *is_connected_arc.write().await = false;
            {
                let mut writer_guard = writer_arc.lock().await;
                *writer_guard = None;
            }
            let _ = app_handle.emit("debug:connection", serde_json::json!({ "connected": false }));
        });
    }

    /// Send a message to the game runtime
    pub async fn send(&self, message: &str) -> Result<(), String> {
        if !self.is_connected().await {
            return Err("Not connected".to_string());
        }

        let encoded = encode_message(message);

        let mut writer_guard = self.writer.lock().await;
        if let Some(ref mut writer) = *writer_guard {
            writer
                .write_all(&encoded)
                .await
                .map_err(|e| format!("Send failed: {}", e))?;
            
            // Log for debugging
            println!("DEBUG: Sent message: {}", message.trim());
        } else {
            return Err("Connection lost".to_string());
        }

        Ok(())
    }

    /// Disconnect from the game runtime
    pub async fn disconnect(&self, app_handle: AppHandle) -> Result<(), String> {
        if !self.is_connected().await {
            return Ok(());
        }

        // Send close message
        let _ = self.send("[closeconnection]\n").await;

        // Wait a bit for the message to be sent
        tokio::time::sleep(tokio::time::Duration::from_millis(200)).await;

        // Close the connection
        *self.is_connected.write().await = false;
        {
            let mut writer_guard = self.writer.lock().await;
            if let Some(mut writer) = writer_guard.take() {
                let _ = writer.shutdown().await;
            }
        }

        // Emit disconnection event
        let _ = app_handle.emit("debug:connection", serde_json::json!({ "connected": false }));

        Ok(())
    }
}
