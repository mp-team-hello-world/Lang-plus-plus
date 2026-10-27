function calculateOptimalHeight() {
    const header = document.querySelector('.header-title');
    const panels = document.querySelector('.panels');
    if (!header || !panels) return 320;

    const headerTop = header.getBoundingClientRect().top;
    const windowHeight = window.innerHeight;
    const panelsTop = panels.getBoundingClientRect().top;
    const bottomOffset = headerTop + 30;

    let availableHeight = windowHeight - panelsTop - bottomOffset;
    const minHeight = 200;
    const maxHeight = 800;
    return Math.max(minHeight, Math.min(maxHeight, availableHeight));
}

function setBothTextareasHeight(height) {
    leftTextarea.style.height = height + 'px';
    leftTextarea.style.overflowY = 'auto';
    rightTextarea.style.height = height + 'px';
    rightTextarea.style.overflowY = 'auto';
}

function updateHeightOnResize() {
    if (isManualResizing) return;
    setBothTextareasHeight(calculateOptimalHeight());
}

function setupManualResize(textarea, otherTextarea, sideName) {
    let wrapper = textarea.parentElement;
    if (!wrapper.classList.contains('textarea-wrapper')) {
        const newWrapper = document.createElement('div');
        newWrapper.className = 'textarea-wrapper';
        textarea.parentNode.insertBefore(newWrapper, textarea);
        newWrapper.appendChild(textarea);
        wrapper = newWrapper;
    }

    let handle = wrapper.querySelector('.resize-handle');
    if (!handle) {
        handle = document.createElement('div');
        handle.className = 'resize-handle';
        wrapper.appendChild(handle);
    }

    handle.style.cursor = 'ns-resize';
    handle.addEventListener('mousedown', function(e) {
        e.preventDefault();
        e.stopPropagation();
        isManualResizing = true;
        const startY = e.clientY;
        const startHeight = textarea.offsetHeight;

        function onMouseMove(moveEvent) {
            const delta = moveEvent.clientY - startY;
            let newHeight = startHeight + delta;
            newHeight = Math.max(150, Math.min(800, newHeight));
            textarea.style.height = newHeight + 'px';
            textarea.style.overflowY = 'auto';
            otherTextarea.style.height = newHeight + 'px';
            otherTextarea.style.overflowY = 'auto';
        }
        function onMouseUp() {
            isManualResizing = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        }
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
}