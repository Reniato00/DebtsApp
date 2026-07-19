window.downloadFile = (fileName, base64Content) => {
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(atob(base64Content));
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.turnstileGetToken = (widgetId) => {
    return turnstile.getResponse(widgetId);
};

window.turnstileReset = (widgetId) => {
    turnstile.reset(widgetId);
};

window.turnstileRender = (containerId, siteKey, dotNetRef) => {
    turnstile.render('#' + containerId, {
        sitekey: siteKey,
        callback: (token) => {
            dotNetRef.invokeMethodAsync('OnTokenCallback', token);
        },
        'expired-callback': () => {
            dotNetRef.invokeMethodAsync('OnTokenExpired');
        }
    });
};