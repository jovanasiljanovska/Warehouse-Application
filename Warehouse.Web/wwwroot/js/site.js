// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//Products/Index
function changeQty(productId, amount) {
    const input = document.getElementById('qty-' + productId);
    let currentValue = parseInt(input.value) || 1;
    let newValue = currentValue + amount;

    // Prevent quantity from going below 1
    if (newValue < 1) newValue = 1;

    input.value = newValue;
}