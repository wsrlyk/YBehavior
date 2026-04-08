import { readFile, writeFile } from '../utils/fileService';
import { getConfigPath } from '../utils/configPath';
import { DefaultTheme, type GraphTheme, applyThemeCssVariables, setTheme } from './theme';

type ThemeOverride = Partial<GraphTheme>;

interface ThemePreset {
    name: string;
    baseTheme?: string;
    theme: ThemeOverride;
}

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
            theme: themeCandidate,
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

        if (typeof parsed.currentTheme === 'string' && parsed.currentTheme.trim()) {
            return parsed.currentTheme.trim();
        }

        const uiMeta = parsed.uiMeta;
        if (isObject(uiMeta) && typeof uiMeta.currentTheme === 'string' && uiMeta.currentTheme.trim()) {
            return uiMeta.currentTheme.trim();
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
    const map = new Map<string, ThemePreset>(presets.map((p) => [p.name, p]));
    const visiting = new Set<string>();

    const resolveByName = (name: string): ThemeOverride => {
        const preset = map.get(name);
        if (!preset) return {};

        if (visiting.has(name)) {
            console.warn(`Theme base inheritance cycle detected at "${name}".`);
            return preset.theme;
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

async function ensureCurrentThemeInMeta(selectedThemeName: string): Promise<void> {
    try {
        const metaPath = await getConfigPath('editor_meta.local.json');

        let parsed: Record<string, unknown> = {};
        try {
            const content = await readFile(metaPath);
            const json = JSON.parse(content) as unknown;
            if (isObject(json)) {
                parsed = json;
            }
        } catch {
            parsed = {};
        }

        const uiMeta = isObject(parsed.uiMeta) ? { ...parsed.uiMeta } : {};
        const rootTheme = typeof parsed.currentTheme === 'string' ? parsed.currentTheme : undefined;
        const uiTheme = typeof uiMeta.currentTheme === 'string' ? uiMeta.currentTheme : undefined;

        if (rootTheme === selectedThemeName && uiTheme === selectedThemeName) {
            return;
        }

        uiMeta.currentTheme = selectedThemeName;
        const next = {
            ...parsed,
            currentTheme: selectedThemeName,
            uiMeta,
        };

        await writeFile(metaPath, JSON.stringify(next, null, 2));
    } catch (e) {
        console.warn('Failed to update editor_meta.local.json currentTheme:', e);
    }
}

export async function initializeThemeFromConfig(): Promise<string> {
    const builtinPresets = await loadThemesFromConfig();
    const localPresets = await loadLocalThemesFromConfig();
    const presets = mergeThemePresets(builtinPresets, localPresets);
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
    applyThemeCssVariables(resolvedTheme);
    await ensureCurrentThemeInMeta(selectedThemeName);

    return selectedThemeName;
}
