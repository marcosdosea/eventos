// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    // Determine the current path and query (if applicable)
    var currentPath = window.location.pathname.toLowerCase();
    var currentSearch = window.location.search.toLowerCase();

    var navLinks = document.querySelectorAll('.offcanvas-body .nav-link.opcao');

    navLinks.forEach(function (link) {
        var href = link.getAttribute('href');
        if (href && href !== '#') {
            // Split the href into path and query
            var parts = href.split('?');
            var linkPath = parts[0].toLowerCase();
            var linkSearch = parts.length > 1 ? '?' + parts[1].toLowerCase() : '';

            // Exact match for path and query parameters
            if (linkPath === currentPath) {
                // If the link has specific query parameters, ensure they match
                if (linkSearch === '' || linkSearch === currentSearch) {
                    link.classList.add('active-opcao');
                }
            }
        }
    });
});