document.addEventListener("DOMContentLoaded", function () {
    const menuToggle = document.getElementById("menu-toggle");
    const menuContent = document.getElementById("menu-content");
    const closeMenu = document.getElementById("close-menu");

    // Set initial state
    menuContent.style.display = "none";

    menuToggle.addEventListener("click", function () {
        if (menuContent.style.display === "block") {
            menuContent.style.display = "none";
        } else {
            menuContent.style.display = "block";
        }
    });

    closeMenu.addEventListener("click", function () {
        menuContent.style.display = "none";
    });
});
