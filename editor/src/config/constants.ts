
// Network Protocol Delimiters
export const FIELD_DELIMITER = '\x04';
export const SECTION_DELIMITER = '\x05';
export const SEQUENCE_DELIMITER = '\x06';
export const LIST_DELIMITER = '\x07';

// Debug Visualization
export const TRANSIENT_HIGHLIGHT_DURATION = 700; // ms

export const DEBUG_COLORS = {
    RUNNING: 'bg-pink-500',
    SUCCESS: 'bg-green-500',
    FAILURE: 'bg-gray-500',
    BREAK: 'bg-red-500',
    DEFAULT: 'bg-gray-500',
};

export const DEBUG_RINGS = {
    RUNNING: 'ring-pink-500',
    SUCCESS: 'ring-green-500',
    FAILURE: 'ring-gray-500',
    BREAK: 'ring-red-500',
    DEFAULT: 'ring-gray-500', // Or whatever default is used
};
