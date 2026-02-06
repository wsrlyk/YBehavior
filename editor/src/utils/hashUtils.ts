/**
 * Hash utilities for YBehavior
 */

/**
 * BKDR Hash function used by YBehavior Runtime
 * Matches Utility.Hash in C# editor
 */
export function bkdrHash(s: string): number {
    let hash = 0;
    for (let i = 0; i < s.length; ++i) {
        // hash = (hash << 5) + hash + s[i]
        // Which is hash * 33 + s[i]
        hash = ((hash << 5) + hash + s.charCodeAt(i)) >>> 0;
    }
    return hash;
}
