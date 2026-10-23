function flashToast(el, duration = 2000) {
    el.classList.remove('show');
    void el.offsetWidth;
    el.classList.add('show');
    setTimeout(() => el.classList.remove('show'), duration);
}

function showInProgressMessage()  { flashToast(progressToast); }
function showSuccessSavedMessage(){ flashToast(successToast);  }
function showUnknownLangMessage() { flashToast(unknownLangToast, 2600); }