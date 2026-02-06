//! Tauri commands for debug network operations.

use std::sync::Arc;
use tauri::{AppHandle, State};
use tokio::sync::RwLock;

use super::connection::ConnectionState;

/// Global connection state managed by Tauri
pub struct DebugConnectionManager {
    state: Arc<RwLock<ConnectionState>>,
}

impl DebugConnectionManager {
    pub fn new() -> Self {
        Self {
            state: Arc::new(RwLock::new(ConnectionState::new())),
        }
    }
}

impl Default for DebugConnectionManager {
    fn default() -> Self {
        Self::new()
    }
}

/// Connect to the game runtime
#[tauri::command]
pub async fn debug_connect(
    app_handle: AppHandle,
    manager: State<'_, DebugConnectionManager>,
    ip: String,
    port: u16,
) -> Result<(), String> {
    let state = manager.state.read().await;
    state.connect(app_handle, ip, port).await
}

/// Disconnect from the game runtime
#[tauri::command]
pub async fn debug_disconnect(
    app_handle: AppHandle,
    manager: State<'_, DebugConnectionManager>,
) -> Result<(), String> {
    let state = manager.state.read().await;
    state.disconnect(app_handle).await
}

/// Send a message to the game runtime
#[tauri::command]
pub async fn debug_send(
    manager: State<'_, DebugConnectionManager>,
    message: String,
) -> Result<(), String> {
    let state = manager.state.read().await;
    state.send(&message).await
}

/// Check if connected to the game runtime
#[tauri::command]
pub async fn debug_is_connected(
    manager: State<'_, DebugConnectionManager>,
) -> Result<bool, String> {
    let state = manager.state.read().await;
    Ok(state.is_connected().await)
}
