// ========================
// DOWNLOAD
// ========================
function openFilenameModal() {
    filenameInput.value = '';
    modal.classList.add('show');
    setTimeout(() => filenameInput.focus(), 100);
}
function closeModal() { modal.classList.remove('show'); }

function triggerDownload() {
    let fileName = filenameInput.value.trim();
    if (fileName === '') fileName = 'document';
    fileName = fileName.replace(/[^a-zA-Z0-9_\-]/g, '_');
    if (fileName.length === 0) fileName = 'downloaded_file';

    const fullFileName = `${fileName}${TARGET_EXT}`;
    let contentToSave = rightTextarea.value;
    if (contentToSave.trim() === '') contentToSave = '// Generated file\n';

    const blob = new Blob([contentToSave], { type: 'text/plain' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fullFileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    showSuccessSavedMessage();
    closeModal();
}