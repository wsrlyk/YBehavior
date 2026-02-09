/**
 * Hash utilities for YBehavior
 */

/**
 * BKDR Hash function used by YBehavior Runtime
 * Matches Utility.Hash in C# editor
 */
export function bkdrHash(s: string): number {
    const bytes = new TextEncoder().encode(s);
    let hash = 0;
    for (let i = 0; i < bytes.length; ++i) {
        // hash = (hash << 5) + hash + bytes[i]
        // Matches runtime's Utility::Hash(const char*)
        hash = ((hash << 5) + hash + bytes[i]) >>> 0;
    }
    return hash;
}
