//! Protocol handling for debug messages.
//! 
//! Message format:
//! - 2 bytes (big-endian): message length (not including these 2 bytes)
//! - N bytes: UTF-8 message content
//!
//! Message content format:
//! - [Header]\x03[Content]
//! - Content uses delimiters: \x04 (field), \x05 (section), \x06 (sequence), \x07 (list)

use serde::{Deserialize, Serialize};

/// A parsed debug message
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DebugMessage {
    pub header: String,
    pub content: String,
}

/// Delimiter characters used in the protocol
pub const HEADER_DELIMITER: char = '\x03';
pub const FIELD_DELIMITER: char = '\x04';
pub const SECTION_DELIMITER: char = '\x05';
pub const SEQUENCE_DELIMITER: char = '\x06';
pub const LIST_DELIMITER: char = '\x07';

/// Decode multiple messages from a byte buffer.
/// Returns (decoded_messages, remaining_bytes)
pub fn decode_messages(data: &[u8]) -> (Vec<DebugMessage>, Vec<u8>) {
    let mut messages = Vec::new();
    let mut index = 0;

    while index < data.len() {
        // Need at least 2 bytes for the length
        if data.len() - index < 2 {
            break;
        }

        // Read message length (big-endian)
        let msg_len = ((data[index] as usize) << 8) | (data[index + 1] as usize);

        // Check if we have the complete message
        if data.len() - index - 2 < msg_len {
            break;
        }

        // Extract message content
        let content_start = index + 2;
        let content_end = content_start + msg_len;
        let content_bytes = &data[content_start..content_end];

        // Parse as UTF-8
        if let Ok(content_str) = std::str::from_utf8(content_bytes) {
            // Split into header and content
            let msg = parse_message(content_str);
            messages.push(msg);
        }

        index = content_end;
    }

    // Return remaining bytes
    let remaining = data[index..].to_vec();
    (messages, remaining)
}

/// Parse a message string into header and content
fn parse_message(msg: &str) -> DebugMessage {
    if let Some(pos) = msg.find(HEADER_DELIMITER) {
        DebugMessage {
            header: msg[..pos].to_string(),
            content: msg[pos + 1..].to_string(),
        }
    } else {
        // No delimiter found, treat entire string as header
        DebugMessage {
            header: msg.to_string(),
            content: String::new(),
        }
    }
}

/// Encode a message string to bytes with length prefix.
/// Note: For simple commands like [Continue], we don't use length prefix,
/// just send the raw text as the old editor does.
pub fn encode_message(msg: &str) -> Vec<u8> {
    // The old C# editor sends raw text without length prefix for outgoing messages
    // Only incoming messages have the length prefix
    msg.as_bytes().to_vec()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_decode_single_message() {
        // Create a test message: length=5, content="hello"
        let data = vec![0x00, 0x05, b'h', b'e', b'l', b'l', b'o'];
        let (messages, remaining) = decode_messages(&data);
        
        assert_eq!(messages.len(), 1);
        assert_eq!(messages[0].header, "hello");
        assert!(remaining.is_empty());
    }

    #[test]
    fn test_decode_message_with_header() {
        // Message with header delimiter
        let content = "[TickResult]\x03some_data";
        let len = content.len();
        let mut data = vec![(len >> 8) as u8, (len & 0xFF) as u8];
        data.extend_from_slice(content.as_bytes());
        
        let (messages, remaining) = decode_messages(&data);
        
        assert_eq!(messages.len(), 1);
        assert_eq!(messages[0].header, "[TickResult]");
        assert_eq!(messages[0].content, "some_data");
        assert!(remaining.is_empty());
    }

    #[test]
    fn test_decode_partial_message() {
        // Only first byte of length
        let data = vec![0x00];
        let (messages, remaining) = decode_messages(&data);
        
        assert!(messages.is_empty());
        assert_eq!(remaining.len(), 1);
    }

    #[test]
    fn test_decode_multiple_messages() {
        // Two messages: "AB" and "CD"
        let data = vec![
            0x00, 0x02, b'A', b'B',
            0x00, 0x02, b'C', b'D',
        ];
        let (messages, remaining) = decode_messages(&data);
        
        assert_eq!(messages.len(), 2);
        assert_eq!(messages[0].header, "AB");
        assert_eq!(messages[1].header, "CD");
        assert!(remaining.is_empty());
    }
}
