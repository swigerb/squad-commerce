let _keyHandler = null;
let _shortcutHandler = null;

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

export function registerKeyboardShortcuts(dotNetRef) {
    _shortcutHandler = (e) => {
        // Skip if user is typing in an input/textarea
        const tag = e.target.tagName;
        const isInput = tag === 'INPUT' || tag === 'TEXTAREA' || e.target.isContentEditable;

        // Esc always works — close any open modal/palette
        if (e.key === 'Escape') {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnShortcutKey', 'Escape');
            return;
        }

        // Skip remaining shortcuts if user is typing
        if (isInput) return;

        // Number keys 1-4 to switch panel focus
        if (['1', '2', '3', '4'].includes(e.key) && !e.ctrlKey && !e.metaKey && !e.altKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnShortcutKey', e.key);
            return;
        }

        // / to focus chat input
        if (e.key === '/' && !e.ctrlKey && !e.metaKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnShortcutKey', '/');
            return;
        }

        // ? to show keyboard help
        if (e.key === '?' || (e.shiftKey && e.key === '/')) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnShortcutKey', '?');
            return;
        }
    };
    document.addEventListener('keydown', _shortcutHandler);
}

export function unregisterKeyboardShortcuts() {
    if (_shortcutHandler) {
        document.removeEventListener('keydown', _shortcutHandler);
        _shortcutHandler = null;
    }
}

export function focusChatInput() {
    const input = document.querySelector('.chat-input');
    if (input) {
        input.focus();
    }
}

export function focusPanel(panelSelector) {
    const panel = document.querySelector(panelSelector);
    if (panel) {
        panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        panel.focus({ preventScroll: true });
    }
}
