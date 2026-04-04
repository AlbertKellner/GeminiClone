// Color logic ported from GeminiClone StructureController.cs
// Original: C# System.Drawing + HSL math — ported to JavaScript

const PREDEFINED_COLORS = [
    "#D32F2F", // Flat Red
    "#1976D2", // Flat Blue
    "#388E3C", // Flat Green
    "#AB47BC", // Flat Magenta
    "#26C6DA", // Flat Cyan
    "#757575", // Flat Black
    "#BDBDBD", // Flat White
    "#5D4037", // Flat Maroon
    "#2E7D32", // Flat Dark Green
    "#1565C0", // Flat Navy
    "#AFB42B", // Flat Olive
    "#7B1FA2", // Flat Purple
    "#00897B", // Flat Teal
    "#BDBDBD", // Flat Silver
    "#F44336", // Flat Red 2
    "#E91E63", // Flat Pink
    "#9C27B0", // Flat Purple 2
    "#673AB7", // Flat Deep Purple
    "#3F51B5", // Flat Indigo
    "#2196F3", // Flat Blue 2
    "#03A9F4", // Flat Light Blue
    "#00BCD4", // Flat Cyan 2
    "#009688", // Flat Teal 2
    "#4CAF50", // Flat Green 2
    "#8BC34A", // Flat Light Green
    "#CDDC39", // Flat Lime
    "#FFEB3B", // Flat Yellow 2
    "#FFC107", // Flat Amber
    "#FF9800", // Flat Orange
    "#FF5722", // Flat Deep Orange
    "#795548", // Flat Brown
    "#9E9E9E", // Flat Grey
    "#607D8B", // Flat Blue Grey
    "#E53935", // Flat Red 3
    "#D81B60", // Flat Pink 2
    "#8E24AA", // Flat Purple 3
    "#5E35B1", // Flat Deep Purple 2
    "#3949AB", // Flat Indigo 2
    "#1E88E5", // Flat Blue 3
    "#039BE5", // Flat Light Blue 2
    "#00ACC1", // Flat Cyan 3
    "#00897B", // Flat Teal 3
    "#43A047", // Flat Green 3
    "#7CB342", // Flat Light Green 2
    "#C0CA33"  // Flat Lime 2
];

function hexToRgb(hex) {
    const h = hex.replace('#', '');
    return {
        r: parseInt(h.substring(0, 2), 16),
        g: parseInt(h.substring(2, 4), 16),
        b: parseInt(h.substring(4, 6), 16)
    };
}

function rgbToHex(r, g, b) {
    const clamp = v => Math.min(255, Math.max(0, Math.round(v)));
    return '#' +
        clamp(r).toString(16).padStart(2, '0') +
        clamp(g).toString(16).padStart(2, '0') +
        clamp(b).toString(16).padStart(2, '0');
}

function rgbToHsl(r, g, b) {
    r /= 255; g /= 255; b /= 255;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = 0, s = 0;
    const l = (max + min) / 2;

    if (max !== min) {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        if (max === r)      h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
        else if (max === g) h = ((b - r) / d + 2) / 6;
        else                h = ((r - g) / d + 4) / 6;
    }
    return { h, s, l };
}

function hslToRgb(h, s, l) {
    if (s === 0) {
        const v = Math.round(l * 255);
        return { r: v, g: v, b: v };
    }
    const hue2rgb = (p, q, t) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 1 / 2) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
    };
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
    const p = 2 * l - q;
    return {
        r: Math.round(hue2rgb(p, q, h + 1 / 3) * 255),
        g: Math.round(hue2rgb(p, q, h) * 255),
        b: Math.round(hue2rgb(p, q, h - 1 / 3) * 255)
    };
}

function interpolateToGrey(hexColor, percentage) {
    const { r, g, b } = hexToRgb(hexColor);
    const grey = (r + g + b) / 3;
    return rgbToHex(
        r + (grey - r) * percentage / 100,
        g + (grey - g) * percentage / 100,
        b + (grey - b) * percentage / 100
    );
}

function saturate(hexColor, percentage) {
    const { r, g, b } = hexToRgb(hexColor);
    let { h, s, l } = rgbToHsl(r, g, b);
    s += (1 - s) * (percentage / 100);
    const rgb = hslToRgb(h, s, l);
    return rgbToHex(rgb.r, rgb.g, rgb.b);
}

function desaturate(hexColor, percentage) {
    const { r, g, b } = hexToRgb(hexColor);
    let { h, s, l } = rgbToHsl(r, g, b);
    s -= s * (percentage / 100);
    const rgb = hslToRgb(h, s, l);
    return rgbToHex(rgb.r, rgb.g, rgb.b);
}

function generateBaseColor(index) {
    const lighterPercentage = 0.4;
    const { r, g, b } = hexToRgb(PREDEFINED_COLORS[index % PREDEFINED_COLORS.length]);
    return rgbToHex(
        Math.min(r + lighterPercentage * 255, 255),
        Math.min(g + lighterPercentage * 255, 255),
        Math.min(b + lighterPercentage * 255, 255)
    );
}

function countDescendants(items, currentDepth = 0) {
    if (!items || items.length === 0) return currentDepth;
    return Math.max(...items.map(item => countDescendants(item.children, currentDepth + 1)));
}

function addColorToChildren(item, color, totalDescendants, depth = 1) {
    const percentage = (depth / (totalDescendants * 1.2)) * 80;
    item.color = interpolateToGrey(color, percentage);

    if (!item.children || item.children.length === 0) {
        item.color = desaturate(item.color, 80);
        return;
    }

    item.children.forEach((child, i) => {
        const childColor = i % 2 === 0
            ? saturate(item.color, 10)
            : saturate(item.color, 30);
        addColorToChildren(child, childColor, totalDescendants, depth + 1);
    });
}
