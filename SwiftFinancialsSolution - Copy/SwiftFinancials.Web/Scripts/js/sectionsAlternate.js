function openSection(sectionId) {
    // Hide all sections
    var sections = document.getElementsByClassName("section");
    for (var i = 0; i < sections.length; i++) {
        sections[i].style.display = "none";
    }

    // Show the selected section
    document.getElementById(sectionId).style.display = "block";
}

function checkTextboxInputs() {
    var textbox1Value = document.getElementById("textbox1").value;
    var textbox2Value = document.getElementById("textbox2").value;

    // If both textboxes are filled, automatically open the second section
    if (textbox1Value.trim() !== "" && textbox2Value.trim() !== "") {
        openSection('section2');
    }
}
