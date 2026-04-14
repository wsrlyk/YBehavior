import { useState, useEffect, useRef } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { useTooltipStore } from '../stores/tooltipStore';
import { stripExtension } from '../utils/fileUtils';
import { getTheme } from '../theme/theme';

interface TreeFilePickerProps {
    value: string;
    onChange: (value: string) => void;
    options: string[];
    placeholder?: string;
    allowJump?: boolean;
    defaultOpen?: boolean;
}

export function TreeFilePicker({ value, onChange, options, placeholder = 'Select a tree...', allowJump = true, defaultOpen = false }: TreeFilePickerProps) {
    const theme = getTheme();
    const [isOpen, setIsOpen] = useState(defaultOpen);
    const [search, setSearch] = useState('');
    const containerRef = useRef<HTMLDivElement>(null);
    const openTree = useEditorStore(state => state.openTree);
    const setTooltip = useTooltipStore(state => state.setTooltip);

    useEffect(() => {
        setIsOpen(defaultOpen);
        if (defaultOpen) {
            setSearch('');
        }
    }, [defaultOpen]);

    const filteredOptions = options.filter(opt =>
        opt.toLowerCase().includes(search.toLowerCase())
    );

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };
        document.addEventListener('mousedown', handleClickOutside);
        return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    return (
        <div className="relative w-full" ref={containerRef}>
            <div
                className="w-full text-xs px-2 py-1 rounded cursor-pointer border flex justify-between items-center group"
                style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
                onMouseEnter={(e) => { e.currentTarget.style.borderColor = theme.ui.accent; }}
                onMouseLeave={(e) => { e.currentTarget.style.borderColor = theme.ui.border; }}
                onClick={() => {
                    setIsOpen(!isOpen);
                    if (!isOpen) setSearch('');
                }}
            >
                <span
                    className="filename-ellipsis flex-1"
                    onMouseEnter={() => value && setTooltip(value)}
                    onMouseLeave={() => setTooltip(null)}
                >
                    {value ? stripExtension(value) : placeholder}
                </span>
                <div className="flex items-center gap-1">
                    {allowJump && value && (
                        <button
                            className="text-[11px] opacity-0 group-hover:opacity-100 transition-opacity"
                            style={{ color: theme.ui.textDim }}
                            onClick={(e) => {
                                e.stopPropagation();
                                openTree(value);
                            }}
                        >
                            ↗
                        </button>
                    )}
                    {value && (
                        <button
                            className="text-[11px] opacity-0 group-hover:opacity-100 transition-opacity"
                            style={{ color: theme.ui.textDim }}
                            onClick={(e) => {
                                e.stopPropagation();
                                onChange('');
                                setIsOpen(false);
                            }}
                        >
                            ✕
                        </button>
                    )}
                    <span className="text-[11px]" style={{ color: theme.ui.textDim }}>▼</span>
                </div>
            </div>

            {isOpen && (
                <div className="absolute z-50 mt-1 w-full border rounded shadow-xl max-h-48 overflow-hidden flex flex-col" style={{ backgroundColor: theme.ui.panelBg, borderColor: theme.ui.border }}>
                    <div className="p-1 border-b" style={{ borderColor: theme.ui.border }}>
                        <input
                            className="w-full text-[11px] px-2 py-1 rounded outline-none border"
                            style={{ backgroundColor: theme.ui.inputBg, color: theme.ui.textMain, borderColor: theme.ui.border }}
                            placeholder="Search trees..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            autoFocus
                        />
                    </div>
                    <div className="overflow-y-auto flex-1 custom-scrollbar">
                        <div
                            className={`px-2 py-1.5 text-[11px] cursor-pointer transition-colors truncate ${!value ? '' : 'italic'}`}
                            style={{
                                backgroundColor: !value ? theme.ui.accentSoft : 'transparent',
                                color: !value ? theme.ui.textMain : theme.ui.textDim
                            }}
                            onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
                            onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = !value ? theme.ui.accentSoft : 'transparent'; }}
                            onClick={() => {
                                onChange('');
                                setIsOpen(false);
                            }}
                        >
                            (None)
                        </div>
                        {filteredOptions.length > 0 ? (
                            filteredOptions.map(opt => (
                                <div
                                    key={opt}
                                    className="px-2 py-1.5 text-[11px] cursor-pointer transition-colors filename-ellipsis"
                                    style={{
                                        color: value === opt || stripExtension(value) === stripExtension(opt) ? theme.ui.textMain : theme.ui.textDim,
                                        backgroundColor: value === opt || stripExtension(value) === stripExtension(opt) ? theme.ui.accentSoft : 'transparent'
                                    }}
                                    onMouseEnter={() => setTooltip(opt)}
                                    onMouseLeave={() => setTooltip(null)}
                                    onMouseOver={(e) => { e.currentTarget.style.backgroundColor = theme.ui.accentSoft; }}
                                    onMouseOut={(e) => {
                                        const selected = value === opt || stripExtension(value) === stripExtension(opt);
                                        e.currentTarget.style.backgroundColor = selected ? theme.ui.accentSoft : 'transparent';
                                    }}
                                    onClick={() => {
                                        // Always store extensionless path in the model for consistency
                                        // and use forward slashes
                                        const normalized = stripExtension(opt).replace(/\\/g, '/');
                                        onChange(normalized);
                                        setIsOpen(false);
                                    }}
                                >
                                    {stripExtension(opt)}
                                </div>
                            ))
                        ) : (
                            <div className="px-2 py-2 text-[11px] italic" style={{ color: theme.ui.textDim }}>No trees found</div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}
