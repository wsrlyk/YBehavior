export function encryptConfigContent(content: string): number[] {
  const source = new TextEncoder().encode(content);
  const data = new Uint8Array(source);
  const size = data.length;

  if (size === 0) {
    return [];
  }

  if (size === 1) {
    data[0] = 0;
    return Array.from(data);
  }

  let prevEncoded = data[1] ^ data[0];
  data[1] = prevEncoded;
  for (let i = 2; i < size; ++i) {
    prevEncoded = data[i] ^ prevEncoded;
    data[i] = prevEncoded;
  }

  data[0] = data[0] ^ data[size - 1];
  return Array.from(data);
}

export function decryptConfigContent(content: number[] | Uint8Array): string {
  const data = content instanceof Uint8Array ? new Uint8Array(content) : new Uint8Array(content);
  const size = data.length;

  if (size > 0) {
    data[0] = data[0] ^ data[size - 1];
    for (let i = size - 1; i > 0; --i) {
      data[i] = data[i] ^ data[i - 1];
    }
  }

  return new TextDecoder('utf-8').decode(data).replace(/^\uFEFF/, '');
}
