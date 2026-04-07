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
        accent: string;       // Primary interactive accent
        accentSoft: string;   // Soft accent background
        buttonBg: string;     // Neutral button background
        buttonHoverBg: string;
        tabBarBg: string;
        tabActiveBg: string;
        tabInactiveBg: string;
        tabActiveText: string;
        tabInactiveText: string;
        tabBorder: string;
        splitterHover: string;
        terminalBg: string;
        terminalText: string;
        terminalHeaderBg: string;
        terminalInputBg: string;
        terminalBorder: string;
        terminalButtonBg: string;
        terminalButtonText: string;
        terminalTimestamp: string;
        terminalLogInfo: string;
        terminalLogSuccess: string;
        terminalLogWarn: string;
        terminalLogError: string;
        terminalLogPurple: string;
        terminalLogOrange: string;
        terminalLogDim: string;
        success: string;
        danger: string;
        warning: string;
    };
}

// ==================== Default Theme (Muted / Low-Contrast) ====================

export const DefaultTheme: GraphTheme = {
    // --- BT Node Categories ---
    // --- BT Node Categories ---
    node: {
        composite: '#BFBFBF',
        decorator: '#BFBFBF',
        action: '#CDB79A',
        condition: '#BBA8BE',
        default: '#BFBFBF',
    },

    // --- Pin Dot Colors ---
    pin: {
        int: '#A35F1E',
        float: '#5F8A2F',
        bool: '#C23A3A',
        string: '#6A46B8',
        vector3: '#B38A1E',
        entity: '#6C7E2E',
        ulong: '#5550AE',
        enum: '#A63A00',
        default: '#6F6F6F',
    },

    // --- Text Colors (hex) ---
    text: {
        pinName: '#2F2F2F',
        variable: '#2F2F2F',
        constant: '#2F2F2F',
        typeColors: {
            int: '#8A4D14',
            float: '#3F6D1F',
            bool: '#A62222',
            string: '#4F3192',
            vector3: '#8A6A13',
            entity: '#4E6120',
            ulong: '#3E3A8A',
            enum: '#7A2A00',
        },
    },

    // --- Edge Colors ---
    edge: {
        tree: {
            default: '#8B8B8B',
            selected: '#1D1D1D',
        },
        data: {
            default: '#7A7A7A',
            selected: '#1D1D1D',
        },
        fsmTransition: {
            default: '#8B8B8B',
            selected: '#6A6A6A',
        },
        label: {
            bg: '#D6D6D6',
            border: '#9A9A9A',
            text: '#111111',
        },
    },

    // --- FSM State Colors ---
    fsmState: {
        Normal: { bg: '#B9B9B9', border: '#888888' },
        Meta: { bg: '#B2A9B6', border: '#7F6D86' },
        Entry: { bg: '#B6B7A9', border: '#7E7F6F' },
        Exit: { bg: '#C4A4A4', border: '#8F6666' },
        Any: { bg: '#C1B59D', border: '#8D7B5C' },
        Upper: { bg: '#B7B0A6', border: '#857A6F' },
    },

    // --- Debug Colors ---
    debug: {
        success: {
            border: '#5D8F5D',
            glow: '0 0 10px rgba(93, 143, 93, 0.45)',
            edge: '#5D8F5D',
        },
        failure: {
            border: '#8A6A6A',
            glow: '0 0 10px rgba(138, 106, 106, 0.45)',
            edge: '#8A6A6A',
        },
        running: {
            border: '#9A6B8F',
            glow: '0 0 10px rgba(154, 107, 143, 0.45)',
            edge: '#9A6B8F',
        },
        break: {
            border: '#AA5A5A',
            glow: '0 0 10px rgba(170, 90, 90, 0.45)',
            edge: '#AA5A5A',
        },
    },

    // --- Context Menu ---
    contextMenu: {
        categoryDots: {
            composite: '#8A9A7A',
            decorator: '#8F8F8F',
            action: '#A98663',
            condition: '#9676A1',
        },
    },

    // --- ReturnType Colors ---
    returnType: {
        Invert: '#8F6A3F',
        Success: '#5D8F5D',
        Failure: '#9A4A4A',
    },

    // --- Comment Colors ---
    comment: {
        bg: '#D8D2C2',
        border: '#9F9481',
        text: '#2B2B2B',
    },

    // --- UI Chrome ---
    ui: {
        background: '#747474',
        panelBg: '#C8C8C8',
        border: '#6E6E6E',
        textMain: '#0A0A0A',
        textDim: '#2F2F2F',
        inputBg: '#FFFFFF',
        gridDots: '#8A8A8A',
        accent: '#696969',
        accentSoft: '#DADADA',
        buttonBg: '#E3E3E3',
        buttonHoverBg: '#EFEFEF',
        tabBarBg: '#7F7F7F',
        tabActiveBg: '#ACACAC',
        tabInactiveBg: '#7F7F7F',
        tabActiveText: '#111111',
        tabInactiveText: '#F2F2F2',
        tabBorder: '#8A8A8A',
        splitterHover: '#696969',
        terminalBg: '#121212',
        terminalText: '#F5F5F5',
        terminalHeaderBg: '#2A2A2A',
        terminalInputBg: '#222222',
        terminalBorder: '#4A4A4A',
        terminalButtonBg: '#3A3A3A',
        terminalButtonText: '#FFFFFF',
        terminalTimestamp: '#D0D0D0',
        terminalLogInfo: '#F5F5F5',
        terminalLogSuccess: '#6DFF7A',
        terminalLogWarn: '#FFD84D',
        terminalLogError: '#FF6B6B',
        terminalLogPurple: '#C77DFF',
        terminalLogOrange: '#FFB454',
        terminalLogDim: '#E6E6E6',
        success: '#6DA56B',
        danger: '#C56A6A',
        warning: '#9E7537',
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

export function applyThemeCssVariables(theme: GraphTheme = activeTheme) {
    if (typeof document === 'undefined') return;

    const root = document.documentElement;

    const vars: Record<string, string> = {
        '--tb-ui-background': theme.ui.background,
        '--tb-ui-panel': theme.ui.panelBg,
        '--tb-ui-border': theme.ui.border,
        '--tb-ui-text-main': theme.ui.textMain,
        '--tb-ui-text-dim': theme.ui.textDim,
        '--tb-ui-input': theme.ui.inputBg,
        '--tb-ui-grid': theme.ui.gridDots,
        '--tb-ui-accent': theme.ui.accent,
        '--tb-ui-accent-soft': theme.ui.accentSoft,
        '--tb-ui-button': theme.ui.buttonBg,
        '--tb-ui-button-hover': theme.ui.buttonHoverBg,
        '--tb-ui-splitter-hover': theme.ui.splitterHover,
        '--tb-ui-terminal': theme.ui.terminalBg,
        '--tb-ui-success': theme.ui.success,
        '--tb-ui-danger': theme.ui.danger,
        '--tb-ui-warning': theme.ui.warning,

        '--tb-color-gray-50': '#f7f7f7',
        '--tb-color-gray-100': '#ececec',
        '--tb-color-gray-200': theme.ui.inputBg,
        '--tb-color-gray-300': theme.ui.panelBg,
        '--tb-color-gray-400': '#A6A6A6',
        '--tb-color-gray-500': theme.ui.border,
        '--tb-color-gray-600': theme.ui.background,
        '--tb-color-gray-700': '#D0D0D0',
        '--tb-color-gray-750': '#C4C4C4',
        '--tb-color-gray-800': '#B8B8B8',
        '--tb-color-gray-900': '#ACACAC',

        '--tb-color-blue-300': theme.ui.accentSoft,
        '--tb-color-blue-200': theme.ui.accentSoft,
        '--tb-color-blue-400': theme.ui.accent,
        '--tb-color-blue-500': theme.ui.accent,
        '--tb-color-blue-600': theme.ui.accent,
        '--tb-color-blue-700': theme.ui.accent,
        '--tb-color-blue-900': theme.ui.accent,
        '--tb-color-green-100': theme.ui.success,
        '--tb-color-green-300': theme.ui.success,
        '--tb-color-green-400': theme.ui.success,
        '--tb-color-green-500': theme.ui.success,
        '--tb-color-green-600': theme.ui.success,
        '--tb-color-green-700': theme.ui.success,
        '--tb-color-red-200': theme.ui.danger,
        '--tb-color-red-300': theme.ui.danger,
        '--tb-color-red-400': theme.ui.danger,
        '--tb-color-red-500': theme.ui.danger,
        '--tb-color-red-600': theme.ui.danger,
        '--tb-color-red-700': theme.ui.danger,
        '--tb-color-red-800': theme.ui.danger,
        '--tb-color-red-900': theme.ui.danger,
        '--tb-color-yellow-300': theme.ui.warning,
        '--tb-color-yellow-400': theme.ui.warning,
        '--tb-color-yellow-500': theme.ui.warning,
        '--tb-color-yellow-600': theme.ui.warning,
        '--tb-color-yellow-700': theme.ui.warning,
        '--tb-color-purple-300': '#9676A1',
        '--tb-color-purple-500': '#9676A1',
        '--tb-color-purple-600': '#9676A1',
        '--tb-color-purple-700': '#9676A1',
        '--tb-color-purple-900': '#9676A1',
        '--tb-color-orange-500': '#A98663',
        '--tb-color-orange-600': '#A98663',
        '--tb-color-orange-700': '#A98663',
        '--tb-color-orange-900': '#A98663',
        '--tb-color-orange-300': '#A98663',
        '--tb-color-pink-300': '#A58DA8',
        '--tb-color-pink-500': '#A58DA8',
        '--tb-color-cyan-500': theme.ui.accent,
        '--tb-color-black': theme.ui.background,
        '--tb-color-white': '#FFFFFF',
    };

    Object.entries(vars).forEach(([key, value]) => {
        root.style.setProperty(key, value);
    });
}
