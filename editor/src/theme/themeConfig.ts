import { readFile } from '../utils/fileService';
import { getConfigPath } from '../utils/configPath';
import { DefaultTheme, type GraphTheme, setTheme } from './theme';

type DeepPartial<T> = {
    [K in keyof T]?: T[K] extends Record<string, unknown> ? DeepPartial<T[K]> : T[K];
};

type ThemeOverride = DeepPartial<GraphTheme>;

interface ThemePreset {
    name: string;
    baseTheme?: string;
    theme: ThemeOverride;
}

let loadedPresets: ThemePreset[] = [];

function isObject(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function cloneDefaultTheme(): GraphTheme {
    return JSON.parse(JSON.stringify(DefaultTheme)) as GraphTheme;
}

function deepMerge<T>(base: T, patch: unknown): T {
    if (!isObject(base) || !isObject(patch)) {
        return (patch as T) ?? base;
    }

    const output: Record<string, unknown> = { ...base };
    for (const [key, value] of Object.entries(patch)) {
        const baseValue = output[key];
        if (isObject(baseValue) && isObject(value)) {
            output[key] = deepMerge(baseValue, value);
        } else {
            output[key] = value;
        }
    }
    return output as T;
}

function sanitizeOverride(base: unknown, patch: unknown, path = 'theme'): unknown {
    if (typeof base === 'string') {
        if (typeof patch === 'string' && patch.trim()) return patch;
        console.warn(`Ignoring invalid ${path}: expected a non-empty string.`);
        return undefined;
    }

    if (!isObject(base) || !isObject(patch)) {
        if (patch !== undefined) console.warn(`Ignoring invalid ${path}: expected an object.`);
        return undefined;
    }

    const result: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(patch)) {
        const baseValue = base[key];
        if (baseValue !== undefined) {
            const sanitized = sanitizeOverride(baseValue, value, `${path}.${key}`);
            if (sanitized !== undefined) result[key] = sanitized;
            continue;
        }

        // Record-based theme sections allow custom categories and value types.
        if (typeof value === 'string' && value.trim()) {
            result[key] = value;
        } else if (isObject(value)) {
            const template = Object.values(base).find(isObject);
            if (template) {
                const sanitized = sanitizeOverride(template, value, `${path}.${key}`);
                if (sanitized !== undefined) result[key] = sanitized;
            }
        } else {
            console.warn(`Ignoring invalid ${path}.${key}.`);
        }
    }
    return result;
}

function parseThemes(raw: unknown): ThemePreset[] {
    let list: unknown[] = [];

    if (Array.isArray(raw)) {
        list = raw;
    } else if (isObject(raw) && Array.isArray(raw.themes)) {
        list = raw.themes;
    }

    const presets: ThemePreset[] = [];
    for (const item of list) {
        if (!isObject(item)) continue;
        const name = item.name;
        if (typeof name !== 'string' || !name.trim()) continue;
        const baseTheme = typeof item.baseTheme === 'string' && item.baseTheme.trim() ? item.baseTheme.trim() : undefined;

        let themeCandidate: ThemeOverride = {};
        if (isObject(item.theme)) {
            themeCandidate = item.theme as ThemeOverride;
        } else {
            const { name: _name, theme: _theme, ...rest } = item;
            themeCandidate = rest as ThemeOverride;
        }

        presets.push({
            name: name.trim(),
            baseTheme,
            theme: (sanitizeOverride(DefaultTheme, themeCandidate) || {}) as ThemeOverride,
        });
    }

    return presets;
}

async function loadCurrentThemeFromMeta(): Promise<string | undefined> {
    try {
        const metaPath = await getConfigPath('editor_meta.local.json');
        const content = await readFile(metaPath);
        const parsed = JSON.parse(content) as unknown;

        if (!isObject(parsed)) return undefined;

        const uiMeta = parsed.uiMeta;
        if (isObject(uiMeta) && typeof uiMeta.currentTheme === 'string' && uiMeta.currentTheme.trim()) {
            return uiMeta.currentTheme.trim();
        }

        // Backward compatibility for metadata written before currentTheme moved under uiMeta.
        if (typeof parsed.currentTheme === 'string' && parsed.currentTheme.trim()) {
            return parsed.currentTheme.trim();
        }

        return undefined;
    } catch {
        return undefined;
    }
}

async function loadThemesFromConfig(): Promise<ThemePreset[]> {
    try {
        const themePath = await getConfigPath('themes.json');
        const content = await readFile(themePath);
        const parsed = JSON.parse(content) as unknown;
        return parseThemes(parsed);
    } catch (e) {
        console.warn('Failed to load themes.json, fallback to DefaultTheme:', e);
        return [];
    }
}

async function loadLocalThemesFromConfig(): Promise<ThemePreset[]> {
    try {
        const themePath = await getConfigPath('themes.local.json');
        const content = await readFile(themePath);
        const parsed = JSON.parse(content) as unknown;
        return parseThemes(parsed);
    } catch {
        return [];
    }
}

function mergeThemePresets(base: ThemePreset[], local: ThemePreset[]): ThemePreset[] {
    const merged = [...base];

    for (const localPreset of local) {
        const index = merged.findIndex((preset) => preset.name === localPreset.name);
        if (index >= 0) {
            merged[index] = {
                name: localPreset.name,
                baseTheme: localPreset.baseTheme ?? merged[index].baseTheme,
                theme: deepMerge(merged[index].theme, localPreset.theme),
            };
        } else {
            merged.push(localPreset);
        }
    }

    return merged;
}

function resolveThemeOverrideWithBase(presets: ThemePreset[], selectedName: string): ThemeOverride {
    const map = new Map<string, ThemePreset>();
    for (const preset of presets) {
        if (map.has(preset.name)) {
            console.warn(`Ignoring duplicate theme name "${preset.name}".`);
            continue;
        }
        map.set(preset.name, preset);
    }
    const visiting = new Set<string>();

    const resolveByName = (name: string): ThemeOverride => {
        const preset = map.get(name);
        if (!preset) {
            console.warn(`Theme "${selectedName}" references missing base theme "${name}".`);
            return {};
        }

        if (visiting.has(name)) {
            console.warn(`Theme base inheritance cycle detected at "${name}".`);
            return {};
        }

        visiting.add(name);
        let resolved: ThemeOverride = {};
        if (preset.baseTheme) {
            resolved = deepMerge(resolved, resolveByName(preset.baseTheme));
        }
        resolved = deepMerge(resolved, preset.theme);
        visiting.delete(name);
        return resolved;
    };

    return resolveByName(selectedName);
}

function resolveTheme(presets: ThemePreset[], name: string): GraphTheme | null {
    if (!presets.some((preset) => preset.name === name)) return null;
    return deepMerge(cloneDefaultTheme(), resolveThemeOverrideWithBase(presets, name));
}

export function getAvailableThemeNames(): string[] {
    return loadedPresets.map((preset) => preset.name);
}

export async function setCurrentTheme(name: string): Promise<boolean> {
    const resolvedTheme = resolveTheme(loadedPresets, name);
    if (!resolvedTheme) return false;

    setTheme(resolvedTheme);
    const { useEditorMetaStore } = await import('../stores/editorMetaStore');
    useEditorMetaStore.getState().setCurrentTheme(name);
    return true;
}

export async function initializeThemeFromConfig(): Promise<string> {
    const builtinPresets = await loadThemesFromConfig();
    const localPresets = await loadLocalThemesFromConfig();
    const presets = mergeThemePresets(builtinPresets, localPresets);
    loadedPresets = presets;
    const currentThemeName = await loadCurrentThemeFromMeta();

    let selectedThemeName = 'default';
    let selectedOverride: ThemeOverride = {};

    if (presets.length > 0) {
        const fallbackPreset = builtinPresets[0] || presets[0];
        const selectedPreset = currentThemeName
            ? presets.find((preset) => preset.name === currentThemeName) || fallbackPreset
            : fallbackPreset;

        selectedThemeName = selectedPreset.name;
        selectedOverride = resolveThemeOverrideWithBase(presets, selectedPreset.name);
    }

    const resolvedTheme = deepMerge(cloneDefaultTheme(), selectedOverride);
    setTheme(resolvedTheme);

    return selectedThemeName;
}
