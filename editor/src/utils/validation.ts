import type { ValueType, CountType } from '../types';

export interface ValidationResult {
    isValid: boolean;
    error?: string;
}

export function validateValue(value: string, type: ValueType, countType: CountType): ValidationResult {
    // Handle array types (list)
    if (countType === 'list') {
        if (value.trim() === '') {
            return { isValid: true }; // Empty array is allowed
        }
        const elements = value.split('|');
        for (const element of elements) {
            const result = validateScalarValue(element, type);
            if (!result.isValid) {
                return { isValid: false, error: `Array element error: ${result.error}` };
            }
        }
        return { isValid: true };
    }

    // Handle scalar types
    return validateScalarValue(value, type);
}

function validateScalarValue(value: string, type: ValueType): ValidationResult {
    const trimmedValue = value.trim();

    switch (type) {
        case 'int':
            if (trimmedValue === '') return { isValid: false, error: 'int cannot be empty' };
            if (!/^-?\d+$/.test(trimmedValue)) return { isValid: false, error: 'int must be an integer' };
            return { isValid: true };

        case 'float':
            if (trimmedValue === '') return { isValid: false, error: 'float cannot be empty' };
            if (isNaN(Number(trimmedValue)) || trimmedValue.includes('|')) return { isValid: false, error: 'float must be a valid number' };
            return { isValid: true };

        case 'string':
            return { isValid: true }; // string can be anything including empty

        case 'vector3':
            if (trimmedValue === '') return { isValid: false, error: 'vector3 cannot be empty' };
            const parts = trimmedValue.split(',');
            if (parts.length !== 3) return { isValid: false, error: 'vector3 must have 3 components separated by comma' };
            for (const part of parts) {
                if (isNaN(Number(part.trim())) || part.trim() === '') {
                    return { isValid: false, error: 'vector3 components must be valid numbers' };
                }
            }
            return { isValid: true };

        case 'bool':
            if (trimmedValue !== 'T' && trimmedValue !== 'F') {
                return { isValid: false, error: 'bool must be T or F' };
            }
            return { isValid: true };

        case 'ulong':
            if (trimmedValue === '') return { isValid: false, error: 'ulong cannot be empty' };
            if (!/^\d+$/.test(trimmedValue)) return { isValid: false, error: 'ulong must be a positive integer' };
            return { isValid: true };

        case 'entity':
            if (trimmedValue !== '') return { isValid: false, error: 'entity must always be empty' };
            return { isValid: true };

        case 'enum':
            return { isValid: true }; // enum usually from dropdown, assuming valid

        default:
            return { isValid: true };
    }
}

export function getDefaultValue(type: ValueType, isList: boolean): string {
    if (isList) return '';
    switch (type) {
        case 'int': return '0';
        case 'float': return '0.0';
        case 'bool': return 'F';
        case 'string': return '';
        case 'vector3': return '0,0,0';
        case 'entity': return '';
        case 'ulong': return '0';
        default: return '';
    }
}
