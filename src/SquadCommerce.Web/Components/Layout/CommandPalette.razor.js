let _keyHandler = null;

export function registerCommandPaletteShortcut(dotNetRef) {
    _keyHandler = (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('Toggle');
        }
    };
    document.addEventListener('keydown', _keyHandler);
}

export function unregisterCommandPaletteShortcut() {
    if (_keyHandler) {
        document.removeEventListener('keydown', _keyHandler);
        _keyHandler = null;
    }
}
