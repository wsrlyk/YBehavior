/**
 * 解码 XML 实体，包括十进制 (&#10;), 十六进制 (&#xD;), 以及标准实体 (&lt;)
 */
export function decodeXmlEntities(str?: string): string {
    if (!str) return '';

    return str
        .replace(/&#(\d+);/g, (_, dec) => String.fromCharCode(parseInt(dec, 10)))
        .replace(/&#x([0-9a-fA-F]+);/g, (_, hex) => String.fromCharCode(parseInt(hex, 16)))
        .replace(/&lt;/g, '<')
        .replace(/&gt;/g, '>')
        .replace(/&quot;/g, '"')
        .replace(/&apos;/g, "'")
        .replace(/&amp;/g, '&');
}
