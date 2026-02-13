/**
 * Centralized Theme System
 * 
 * All graph-related colors are defined here for easy customization.
 * Future: support multiple themes / user-editable colors.
 */

// ==================== Theme Interface ====================

export interface GraphTheme {
    /** BT Node category background colors */
    node: Record<string, string>;

    /** Pin dot colors by value type */
    pin: Record<string, string>;

    /** Text colors (hex) */
    text: {
        pinName: string;
        variable: string;
        constant: string;
        /** Type label colors in PropertiesPanel (hex) */
        typeColors: Record<string, string>;
    };

    /** Edge colors */
    edge: {
        tree: { default: string; selected: string };
        data: { default: string; selected: string };
        fsmTransition: { default: string; selected: string };
        label: { bg: string; border: string; text: string };
    };

    /** FSM state node colors */
    fsmState: Record<string, { bg: string; border: string }>;

    /** Debug visualization colors */
    debug: {
        success: { border: string; glow: string; edge: string };
        failure: { border: string; glow: string; edge: string };
        running: { border: string; glow: string; edge: string };
        break: { border: string; glow: string; edge: string };
    };

    /** Context menu category dot colors */
    contextMenu: {
        categoryDots: Record<string, string>;
    };

    /** ReturnType label colors */
    returnType: Record<string, string>;

    /** Comment colors */
    comment: {
        bg: string;
        border: string;
        text: string;
    };

    /** UI Chrome colors */
    ui: {
        background: string;   // Main window/canvas background
        panelBg: string;      // Sidebar, Properties, Toolbar
        border: string;       // Panel borders
        textMain: string;
        textDim: string;
        inputBg: string;      // Input field background
        gridDots: string;     // Canvas dot pattern
    };
}

// ==================== Default Theme (Muted / Low-Contrast) ====================

export const DefaultTheme: GraphTheme = {
    // --- BT Node Categories ---
    // --- BT Node Categories ---
    node: {
        composite: '#4F7F55',   // Muted Forest Green
        decorator: '#525252',   // Neutral-600
        action: '#A0734A',   // Muted Bronze
        condition: '#6B4D78',   // Muted Plum
        default: '#525252',  // Neutral-600 (was #555555)
    },

    // --- Pin Dot Colors ---
    pin: {
        int: '#6B8EAD',   // Steel Blue
        float: '#759E6B',   // Moss Green
        bool: '#B55C5C',   // Brick Red
        string: '#BFA0C4',   // Mauve
        vector3: '#BDB066',   // Ochre
        entity: '#66BDBD',   // Teal
        ulong: '#8888B5',   // Slate
        enum: '#BA8B5D',   // Leather
        default: '#777777',
    },

    // --- Text Colors (hex) ---
    text: {
        pinName: '#9ca3af',   // gray-400
        variable: '#89b4fa',   // Catppuccin Blue (soft)
        constant: '#9ca3af',   // gray-400
        typeColors: {
            int: '#6B8EAD',
            float: '#759E6B',
            bool: '#B55C5C',
            string: '#BFA0C4',
            vector3: '#BDB066',
            entity: '#66BDBD',
            ulong: '#8888B5',
            enum: '#BA8B5D',
        },
    },

    // --- Edge Colors ---
    edge: {
        tree: {
            default: '#737373',  // Neutral-500 (was #6b7280 which is Gray-500/Cool)
            selected: '#ffffff',
        },
        data: {
            default: '#7AABDE',  // Softer blue (was #60a5fa)
            selected: '#ffffff',
        },
        fsmTransition: {
            default: '#737373',  // Neutral-500 (was #718096 Slate-500)
            selected: '#7AABDE',  // Keep blue for selection
        },
        label: {
            bg: '#404040',    // Neutral-700 (was #374151 Gray-700)
            border: '#525252',// Neutral-600 (was #4b5563 Gray-600)
            text: '#e5e5e5',  // Neutral-200 (was #e5e7eb Gray-200)
        },
    },

    // --- FSM State Colors ---
    fsmState: {
        Normal: { bg: '#404040', border: '#737373' }, // Neutral-700 / Neutral-500
        Meta: { bg: '#553C9A', border: '#805AD5' },
        Entry: { bg: '#276749', border: '#48BB78' },
        Exit: { bg: '#9B2C2C', border: '#FC8181' },
        Any: { bg: '#744210', border: '#D69E2E' },
        Upper: { bg: '#1E3A8A', border: '#3B82F6' }, // Dark Blue / Blue
    },

    // --- Debug Colors ---
    debug: {
        success: {
            border: '#4ade80',
            glow: '0 0 12px rgba(74, 222, 128, 0.5)',
            edge: '#4ade80',
        },
        failure: {
            border: '#60a5fa',
            glow: '0 0 12px rgba(96, 165, 250, 0.5)',
            edge: '#60a5fa',
        },
        running: {
            border: '#f472b6',
            glow: '0 0 12px rgba(244, 114, 182, 0.5)',
            edge: '#f472b6',
        },
        break: {
            border: '#ef4444',
            glow: '0 0 12px rgba(239, 68, 68, 0.5)',
            edge: '#ef4444',
        },
    },

    // --- Context Menu ---
    contextMenu: {
        categoryDots: {
            composite: '#4F7F55',
            decorator: '#525252', // Neutral-600
            action: '#A0734A',
            condition: '#6B4D78',
        },
    },

    // --- ReturnType Colors ---
    returnType: {
        Invert: '#c05621',  // Muted Orange (was bg-orange-500)
        Success: '#2f855a', // Muted Green (was bg-green-600)
        Failure: '#c53030', // Muted Red (was bg-red-600)
    },

    // --- Comment Colors ---
    // Low saturation warm tones to blend with dark neutral theme
    comment: {
        bg: '#25221b',      // Very dark warm gray (was #422006)
        border: '#454030',  // Muted olive/gold (was #ca8a04)
        text: '#d1cdc4',    // Muted beige (was #fef3c7)
    },

    // --- UI Chrome ---
    ui: {
        background: '#171717',    // Neutral-900 (Main Canvas)
        panelBg: '#262626',       // Neutral-800 (Panels)
        border: '#404040',        // Neutral-700 (Borders)
        textMain: '#e5e5e5',      // Neutral-200
        textDim: '#a3a3a3',       // Neutral-400
        inputBg: '#171717',       // Neutral-900 (Inputs)
        gridDots: '#404040',      // Neutral-700 (Dots)
    },
};

// ==================== Active Theme ====================
// Currently just the default. Future: load from settings / allow runtime switch.

let activeTheme: GraphTheme = DefaultTheme;

export function getTheme(): GraphTheme {
    return activeTheme;
}

export function setTheme(theme: GraphTheme) {
    activeTheme = theme;
}
