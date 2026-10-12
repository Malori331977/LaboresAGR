window.qrScanner = {
    startCamera: async function (videoElementId) {
        const video = document.getElementById(videoElementId);
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });
        video.srcObject = stream;
        await video.play();
    },
    scanQRCode: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        const canvas = document.createElement("canvas");
        const context = canvas.getContext("2d");

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
        const code = jsQR(imageData.data, canvas.width, canvas.height);
        console.log(code)
        return code ? code.data : null;
    },
    stopCamera: function (videoElementId) {
        const video = document.getElementById(videoElementId);
        const stream = video.srcObject;
        if (stream) {
            const tracks = stream.getTracks();
            tracks.forEach(track => track.stop());
        }
        video.srcObject = null;
    }
};
