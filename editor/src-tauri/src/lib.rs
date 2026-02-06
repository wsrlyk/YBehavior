use std::fs;
use std::path::PathBuf;
use std::env;
use tauri::Manager;

mod network;

use network::{debug_connect, debug_disconnect, debug_send, debug_is_connected, DebugConnectionManager};

#[tauri::command]
fn get_exe_dir() -> Result<String, String> {
    let exe_path = env::current_exe().map_err(|e| e.to_string())?;
    let exe_dir = exe_path.parent().ok_or("Cannot get exe directory")?;
    Ok(exe_dir.to_string_lossy().to_string())
}

#[tauri::command]
fn read_file(path: String) -> Result<String, String> {
    fs::read_to_string(&path).map_err(|e| e.to_string())
}

#[tauri::command]
fn write_file(path: String, content: String) -> Result<(), String> {
    let path = std::path::Path::new(&path);
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|e| e.to_string())?;
    }
    fs::write(path, content).map_err(|e| e.to_string())
}

#[tauri::command]
fn list_files(dir: String, extensions: Vec<String>) -> Result<Vec<String>, String> {
    let path = PathBuf::from(&dir);
    if !path.is_dir() {
        return Err("Not a directory".to_string());
    }
    
    let mut files = Vec::new();
    collect_files(&path, &path, &extensions, &mut files)?;
    Ok(files)
}

fn collect_files(
    base: &PathBuf,
    dir: &PathBuf,
    extensions: &[String],
    files: &mut Vec<String>,
) -> Result<(), String> {
    let entries = fs::read_dir(dir).map_err(|e| e.to_string())?;
    
    for entry in entries {
        let entry = entry.map_err(|e| e.to_string())?;
        let path = entry.path();
        
        if path.is_dir() {
            collect_files(base, &path, extensions, files)?;
        } else if let Some(ext) = path.extension() {
            let ext_str = ext.to_string_lossy().to_string();
            if extensions.iter().any(|e| e == &ext_str) {
                if let Ok(relative) = path.strip_prefix(base) {
                    files.push(relative.to_string_lossy().to_string());
                }
            }
        }
    }
    Ok(())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .manage(DebugConnectionManager::new())
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::CloseRequested { .. } = event {
                if window.label() == "main" {
                    let app_handle = window.app_handle().clone();
                    // Close all other windows first
                    for (label, win) in app_handle.webview_windows() {
                        if label != "main" {
                            let _ = win.destroy();
                        }
                    }
                    // The main window will close after this handler, 
                    // and since it's the last one, the app will exit naturally.
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            get_exe_dir, 
            read_file, 
            write_file, 
            list_files,
            debug_connect,
            debug_disconnect,
            debug_send,
            debug_is_connected
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
