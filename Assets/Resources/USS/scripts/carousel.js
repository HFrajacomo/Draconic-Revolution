document.addEventListener("DOMContentLoaded", function () {
    let clicked = false;
    const prevButton = document.getElementById("prev-button");
    const nextButton = document.getElementById("next-button");
    const carouselItem = document.querySelector(".carousel-item");
    const carousel = document.querySelector(".carousel");
    carousel.addEventListener('scroll', () => {
        const isStart = carousel.scrollLeft === 0;
        const isEnd = carousel.scrollLeft >= carousel.scrollWidth - carousel.offsetWidth;
        if (isStart) {
            prevButton.classList.replace('button-primary', 'button-disabled');
            nextButton.classList.replace('button-disabled', 'button-primary');
        } else if (isEnd) {
            prevButton.classList.replace('button-disabled', 'button-primary');
            nextButton.classList.replace('button-primary', 'button-disabled');
        } else {
            prevButton.classList.replace('button-disabled', 'button-primary');
            nextButton.classList.replace('button-disabled', 'button-primary');
        }
    });
    prevButton.addEventListener("click", function () {
        if (clicked === false) {
            carousel.scrollBy({
                left: -carouselItem.offsetWidth - 45,
                behavior: "smooth",
            });
            clicked = true
            setTimeout(() => { clicked = false }, 400);
        }
    });
    nextButton.addEventListener("click", function () {
        if (clicked === false) {
            carousel.scrollBy({
                left: carouselItem.offsetWidth + 45,
                behavior: "smooth",
            });
            clicked = true
            setTimeout(() => { clicked = false }, 400);
        }
    });
});