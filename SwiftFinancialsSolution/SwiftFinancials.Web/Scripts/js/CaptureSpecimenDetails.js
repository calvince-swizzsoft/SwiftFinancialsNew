// Function to trigger file browsing for image upload
function browseImage(inputId, placeholderId) {
    document.getElementById(inputId).click();
}

// Function to display the selected image in the placeholder
function showImage(inputId, placeholderId) {
    const input = document.getElementById(inputId);
    const placeholder = document.getElementById(placeholderId);

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            placeholder.innerHTML = '<img src="' + e.target.result + '" />';
        }
        reader.readAsDataURL(input.files[0]);
    }
}

// Function to capture image from webcam
function captureWebcamImage() {
    const video = document.getElementById('webcam');
    const canvas = document.getElementById('canvas');
    const placeholder = document.getElementById('passportImagePlaceholder');

    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        navigator.mediaDevices.getUserMedia({ video: true }).then(function (stream) {
            video.srcObject = stream;
            video.play();
            video.style.display = 'block';

            // Capture image when video is clicked
            video.addEventListener('click', function () {
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                canvas.getContext('2d').drawImage(video, 0, 0);
                const imageData = canvas.toDataURL('image/png');
                placeholder.innerHTML = '<img src="' + imageData + '" />';
                video.pause();
                video.style.display = 'none';
                stream.getTracks().forEach(track => track.stop());
                // Disable further capturing after one photo is taken
                video.removeEventListener('click', arguments.callee);
            });
        });
    } else {
        alert("Your browser doesn't support camera access.");
    }
}

// Function to remove image from the placeholder and reset input
function removeImage(placeholderId, inputId, webcamId = null) {
    const placeholder = document.getElementById(placeholderId);
    const fileInput = document.getElementById(inputId);

    // Clear the placeholder
    placeholder.innerHTML = '';

    // Reset the file input
    fileInput.value = '';

    // If there's a webcam, stop it and hide the video
    if (webcamId) {
        const webcam = document.getElementById(webcamId);
        webcam.srcObject = null;
        webcam.style.display = 'none';
    }
}

