function enlargeImage(img) {
    const enlarged = document.getElementById('enlargedImage');
    const largeImg = document.getElementById('largeImg');
    largeImg.src = img.src; 
    enlarged.style.display = 'flex'; 
}