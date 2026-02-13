export function generateGUID(existingGUIDs: Set<number>): number {
    let guid: number;
    do {
        // Generate a secure random uint32
        const array = new Uint32Array(1);
        crypto.getRandomValues(array);
        guid = array[0];
    } while (existingGUIDs.has(guid) || guid === 0);
    return guid;
}
