import { useState, useEffect, useRef } from 'react';
import { useEditorStore } from '../stores/editorStore';
import { stripExtension } from '../utils/fileUtils';

interface TreeFilePickerProps {
    value: string;
    onChange: (value: string) => void;
    options: string[];
    placeholder?: string;
    allowJump?: boolean;
}

export function TreeFilePicker({ value, onChange, options, placeholder = 'Select a tree...', allowJump = true }: TreeFilePickerProps) {
    const [isOpen, setIsOpen] = useState(false);
    const [search, setSearch] = useState('');
    const containerRef = useRef<HTMLDivElement>(null);
    const openTree = useEditorStore(state => state.openTree);

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
                className="w-full bg-gray-700 text-gray-300 text-xs px-2 py-1 rounded cursor-pointer border border-gray-600 hover:border-blue-500 flex justify-between items-center group"
                onClick={() => {
                    setIsOpen(!isOpen);
                    if (!isOpen) setSearch('');
                }}
            >
                <span className="filename-ellipsis flex-1" title={value}>
                    {value ? stripExtension(value) : placeholder}
                </span>
                <div className="flex items-center gap-1">
                    {allowJump && value && (
                        <button
                            className="text-[10px] text-gray-500 hover:text-blue-400 opacity-0 group-hover:opacity-100 transition-opacity"
                            onClick={(e) => {
                                e.stopPropagation();
                                openTree(value);
                            }}
                            title="Open this tree"
                        >
                            ↗
                        </button>
                    )}
                    {value && (
                        <button
                            className="text-[10px] text-gray-500 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity"
                            onClick={(e) => {
                                e.stopPropagation();
                                onChange('');
                                setIsOpen(false);
                            }}
                            title="Clear selection"
                        >
                            ✕
                        </button>
                    )}
                    <span className="text-[10px] text-gray-500">▼</span>
                </div>
            </div>

            {isOpen && (
                <div className="absolute z-50 mt-1 w-full bg-gray-800 border border-gray-700 rounded shadow-xl max-h-48 overflow-hidden flex flex-col">
                    <div className="p-1 border-b border-gray-700">
                        <input
                            className="w-full bg-gray-900 text-gray-300 text-[10px] px-2 py-1 rounded outline-none border border-gray-600 focus:border-blue-500"
                            placeholder="Search trees..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            autoFocus
                        />
                    </div>
                    <div className="overflow-y-auto flex-1 custom-scrollbar">
                        <div
                            className={`px-2 py-1.5 text-[10px] cursor-pointer hover:bg-blue-600 hover:text-white transition-colors truncate ${!value ? 'bg-blue-900 text-blue-200' : 'text-gray-500 italic'}`}
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
                                    className={`px-2 py-1.5 text-[10px] cursor-pointer hover:bg-gray-700 transition-colors filename-ellipsis ${value === opt || stripExtension(value) === stripExtension(opt) ? 'text-blue-400 bg-gray-750' : 'text-gray-300'}`}
                                    title={opt}
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
                            <div className="px-2 py-2 text-[10px] text-gray-500 italic">No trees found</div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}
