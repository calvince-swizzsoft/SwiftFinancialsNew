document.addEventListener('DOMContentLoaded', () => {
    const marquee = document.querySelector('.marquee span');
    
    marquee.addEventListener('mouseover', () => {
        marquee.style.transform = 'rotateY(0deg) scale(1.2)'; // Enhance 3D effect on hover
    });

    marquee.addEventListener('mouseout', () => {
        marquee.style.transform = 'rotateY(10deg) scale(1)'; // Reset effect
    });
});
