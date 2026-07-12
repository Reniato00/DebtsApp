window.downloadFile = (fileName, base64Content) => {
    const link = document.createElement('a');
    link.href = 'data:text/csv;charset=utf-8,' + encodeURIComponent(atob(base64Content));
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};