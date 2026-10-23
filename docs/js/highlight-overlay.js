const HLJS_LANG_MAP = {
    kt: 'kotlin',
    sharp: 'csharp',
    python: 'python',
    cpp: 'cpp'
};

let highlightSyncScheduled = false;

function updateHighlight(textarea, highlightLayer, langClass) {
    if (!highlightLayer) return;
    const codeEl = highlightLayer.querySelector('code');
    if (!codeEl) return;

    codeEl.textContent = textarea.value;
    codeEl.className = `language-${langClass}`;

    if (typeof hljs !== 'undefined') {
        delete codeEl.dataset.highlighted;
        hljs.highlightElement(codeEl);
    }

    highlightLayer.scrollTop = textarea.scrollTop;
    highlightLayer.scrollLeft = textarea.scrollLeft;
}

function scheduleHighlightUpdate(textarea, highlightLayer, langClass) {
    if (highlightSyncScheduled) return;
    highlightSyncScheduled = true;
    requestAnimationFrame(() => {
        highlightSyncScheduled = false;
        updateHighlight(textarea, highlightLayer, langClass);
    });
}

function getLeftLangClass() {
    if (!sourceLanguage) return 'plaintext';
    return HLJS_LANG_MAP[sourceLanguage] || 'plaintext';
}

function initHighlighting() {
    const leftLayer = document.getElementById('leftHighlight');
    const rightLayer = document.getElementById('rightHighlight');

    if (!leftLayer || !rightLayer) return;

    leftTextarea.addEventListener('scroll', () => {
        leftLayer.scrollTop = leftTextarea.scrollTop;
        leftLayer.scrollLeft = leftTextarea.scrollLeft;
    });

    rightTextarea.addEventListener('scroll', () => {
        rightLayer.scrollTop = rightTextarea.scrollTop;
        rightLayer.scrollLeft = rightTextarea.scrollLeft;
    });

    leftTextarea.addEventListener('input', () => {
        scheduleHighlightUpdate(leftTextarea, leftLayer, getLeftLangClass());
    });

    rightTextarea.addEventListener('input', () => {
        scheduleHighlightUpdate(rightTextarea, rightLayer, 'cpp');
    });

    updateHighlight(leftTextarea, leftLayer, getLeftLangClass());
    updateHighlight(rightTextarea, rightLayer, 'cpp');
}

function refreshLeftHighlight() {
    const leftLayer = document.getElementById('leftHighlight');
    if (leftLayer) {
        updateHighlight(leftTextarea, leftLayer, getLeftLangClass());
    }
}

function refreshRightHighlight() {
    const rightLayer = document.getElementById('rightHighlight');
    if (rightLayer) {
        updateHighlight(rightTextarea, rightLayer, 'cpp');
    }
}