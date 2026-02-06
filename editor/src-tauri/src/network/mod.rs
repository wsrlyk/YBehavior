//! Network module for debug communication with game runtime.
//! 
//! This module provides TCP-based communication with the game runtime
//! for debugging behavior trees and FSMs.

mod connection;
mod protocol;
mod commands;

pub use commands::*;
