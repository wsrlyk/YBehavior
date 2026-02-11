// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::env;
use std::fs;
use std::path::PathBuf;
use std::process::Command;
use std::thread;
use std::time::{Duration, SystemTime};

fn main() {
    let args: Vec<String> = env::args().collect();

    // Update Mode: --update --target <path>
    if let Some(_) = args.iter().position(|a| a == "--update") {
        let target = args.iter().position(|a| a == "--target")
            .and_then(|i| args.get(i + 1))
            .map(PathBuf::from);

        if let Some(target_path) = target {
            run_update(target_path);
        }
        return;
    }

    // Normal Mode: check for updates, then start Tauri
    if let Some(source_path) = check_for_update() {
        // Source exe is different from current exe → launch source in update mode
        if let Ok(current_exe) = env::current_exe() {
            let _ = Command::new(&source_path)
                .args(["--update", "--target", &current_exe.to_string_lossy()])
                .spawn();
            return; // Exit so the updater can overwrite us
        }
    }

    ai_editor_lib::run();
}

/// Check if sourceExePath is configured and if the source exe differs from us.
/// Returns Some(source_path) if an update is needed, None otherwise.
fn check_for_update() -> Option<PathBuf> {
    let exe_path = env::current_exe().ok()?;
    let exe_dir = exe_path.parent()?;

    // Read settings.json from config directory
    let settings_path = exe_dir.join("config").join("settings.json");
    let content = fs::read_to_string(&settings_path).ok()?;
    let raw: serde_json::Value = serde_json::from_str(&content).ok()?;

    let source_rel = raw.get("sourceExePath")?.as_str()?;
    if source_rel.is_empty() {
        return None;
    }

    // Resolve relative path
    let source_path = exe_dir.join(source_rel);
    
    // Check if source path exists and is not the same as the current exe
    if !source_path.exists() {
        return None;
    }

    if let (Ok(c), Ok(s)) = (exe_path.canonicalize(), source_path.canonicalize()) {
        if c == s {
            return None; // Source and current are the same file, skip update
        }
    }

    // Compare file metadata (Size + MTime)
    // If metadata matches, we usually don't need to update.
    if let (Some(c), Some(s)) = (get_file_unique_id(&exe_path), get_file_unique_id(&source_path)) {
        if c != s {
            return Some(source_path);
        }
    }

    None
}

/// Returns a "signature" of the file: (size, modified_time)
fn get_file_unique_id(path: &PathBuf) -> Option<(u64, SystemTime)> {
    let metadata = fs::metadata(path).ok()?;
    Some((metadata.len(), metadata.modified().ok()?))
}

/// Update Mode: wait for target to be unlocked, copy self there, relaunch.
fn run_update(target: PathBuf) {
    let current = match env::current_exe() {
        Ok(p) => p,
        Err(_) => return,
    };

    // Wait for the target file to become writable (old process exiting)
    for _ in 0..100 {
        match fs::copy(&current, &target) {
            Ok(_) => {
                // Successfully copied, now relaunch
                let _ = Command::new(&target).spawn();
                return;
            }
            Err(_) => {
                thread::sleep(Duration::from_millis(100));
            }
        }
    }

    // Timeout - failed to update, launch target anyway so user isn't stuck
    let _ = Command::new(&target).spawn();
}
