use std::env;
use std::fs;
use std::path::Path;

fn main() {
    tauri_build::build();

    let manifest_dir = env::var("CARGO_MANIFEST_DIR").unwrap();
    let source_config = Path::new(&manifest_dir).join("../config");
    
    // Determine target directory (hacky but works for standard layout)
    let profile = env::var("PROFILE").unwrap(); // "debug" or "release"
    let target_dir = Path::new(&manifest_dir).join("target").join(&profile);
    
    let dest_config = target_dir.join("config");
    
    // Ensure target dir exists (it should, but just in case)
    if !target_dir.exists() {
        fs::create_dir_all(&target_dir).unwrap();
    }
    
    if source_config.exists() {
        if !dest_config.exists() {
            fs::create_dir_all(&dest_config).unwrap();
        }
        
        for entry in fs::read_dir(source_config).unwrap() {
            let entry = entry.unwrap();
            let path = entry.path();
            if path.is_file() {
                let file_name = path.file_name().unwrap();
                let dest_path = dest_config.join(file_name);
                fs::copy(&path, &dest_path).unwrap();
            }
        }
        println!("cargo:rerun-if-changed=../config");
    }
}
