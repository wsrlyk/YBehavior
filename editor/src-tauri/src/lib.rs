use std::fs;
use std::path::PathBuf;
use std::env;
use tauri::Manager;
use tauri::Emitter;

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
fn exit_app(app: tauri::AppHandle) {
    app.exit(0);
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
        .setup(|app| {
            // Start source exe watcher if configured
            start_source_watcher(app.handle().clone());
            Ok(())
        })
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
            debug_is_connected,
            exit_app
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

/// Start a background thread that watches the source exe for changes.
/// Reads `sourceExePath` from settings.json. If not configured, does nothing.
fn start_source_watcher(handle: tauri::AppHandle) {
    let exe_path = match env::current_exe() {
        Ok(p) => p,
        Err(_) => return,
    };
    let exe_dir = match exe_path.parent() {
        Some(d) => d.to_path_buf(),
        None => { println!("[Watcher] Error: Could not get exe parent dir."); return; },
    };

    // Read settings.json
    let settings_path = exe_dir.join("config").join("settings.json");
    let content = match fs::read_to_string(&settings_path) {
        Ok(c) => c,
        Err(e) => { println!("[Watcher] Skip: Could not read {:?}: {}", settings_path, e); return; },
    };
    let raw: serde_json::Value = match serde_json::from_str(&content) {
        Ok(v) => v,
        Err(e) => { println!("[Watcher] Error: settings.json parse failed: {}", e); return; },
    };

    let source_rel = match raw.get("sourceExePath").and_then(|v| v.as_str()) {
        Some(s) if !s.is_empty() => s.to_string(),
        _ => { println!("[Watcher] Skip: sourceExePath not configured in settings.json."); return; },
    };

    let source_path = match exe_dir.join(&source_rel).canonicalize() {
        Ok(p) if p.exists() => p,
        _ => { println!("[Watcher] Error: sourceExePath does not exist or is invalid: {}", source_rel); return; },
    };

    // Skip watcher if source path is the same as the current exe
    if let Ok(current_canonical) = exe_path.canonicalize() {
        if current_canonical == source_path {
            return;
        }
    }

    // Get initial file stats
    let initial_stats = fs::metadata(&source_path).ok().and_then(|m| {
        Some((m.len(), m.modified().ok()?))
    });
    
    if initial_stats.is_none() {
        return;
    }
    let initial_stats = initial_stats.unwrap();

    // Spawn watcher thread
    std::thread::spawn(move || {
        loop {
            std::thread::sleep(std::time::Duration::from_secs(300));

            if let Ok(metadata) = fs::metadata(&source_path) {
                if let Ok(modified) = metadata.modified() {
                    let current_stats = (metadata.len(), modified);
                    if current_stats != initial_stats {
                        // Source exe has changed
                        let _ = handle.emit("update-available", ());
                        break; // Only notify once
                    }
                }
            }
        }
    });
}

