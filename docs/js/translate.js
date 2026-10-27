function updateTranslateButtonState() {
    translateBtn.disabled =
        leftTextarea.value.trim() === '' || sourceLanguage === null;
}

async function translateText() {
    if (translateBtn.disabled) return;

    const payload = {
        source: sourceLanguage,
        target: targetLanguage,
        code: leftTextarea.value
    };

    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => { translateBtn.style.transform = ''; }, 120);

    try {
        const tunnelUrl = 'https://lang-plus-plus-mkn-26.loca.lt/translate';
        // const tunnelUrl = 'http://localhost:5000/translate' //- для локального тестирования

        const response = await fetch(tunnelUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Bypass-Tunnel-Reminder': 'true'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Ошибка компиляции');
        }

        const data = await response.json();
        rightTextarea.value = data.cppCode;

        lastTreeData = data.tree || null;
        viewTreeBtn.disabled = !lastTreeData;

        if (typeof refreshRightHighlight === 'function') {
            refreshRightHighlight();
        }

    } catch (error) {
        console.error("Ошибка трансляции:", error);
        rightTextarea.value = `/* \n[ОШИБКА ТРАНСЛЯЦИИ]\nНе удалось связаться с сервером или парсер ANTLR обнаружил ошибку:\n${error.message}\n*/`;

        if (typeof refreshRightHighlight === 'function') {
            refreshRightHighlight();
        }

        lastTreeData = null;
        viewTreeBtn.disabled = true;
    } finally {
        if (typeof autoResize === 'function') autoResize(rightTextarea);
    }
}