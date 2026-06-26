
// JavaScript to handle toggling the section and changing button text
const toggleButton = document.getElementById('toggleButton');
const section = document.getElementById('section');

const toggleButton2 = document.getElementById('toggleButton2');
const section2 = document.getElementById('section2');

const toggleButton3 = document.getElementById('toggleButton3');
const section3 = document.getElementById('section3');


toggleButton.addEventListener('click', function () {
    // Toggle the visibility of the section
    section.classList.toggle('open');

    // Change the button text based on the section's visibility
    if (section.classList.contains('open')) {
        toggleButton.textContent = 'Close';
    } else {
        toggleButton.textContent = 'Open';
    }
});

toggleButton2.addEventListener('click', function () {
    // Toggle the visibility of the section
    section2.classList.toggle('open');

    // Change the button text based on the section's visibility
    if (section2.classList.contains('open')) {
        toggleButton2.textContent = 'Close';
    } else {
        toggleButton2.textContent = 'Open';
    }
});

toggleButton3.addEventListener('click', function () {
    // Toggle the visibility of the section
    section3.classList.toggle('open');

    // Change the button text based on the section's visibility
    if (section3.classList.contains('open')) {
        toggleButton3.textContent = 'Close';
    } else {
        toggleButton3.textContent = 'Open';
    }
});