// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
	// Edit/delete flows are handled by page-specific scripts (Expense, Category)
	// which load partials into their own offcanvas. No global offcanvas.
});

/** Initialize category form icon picker and color sync (call after loading partial into offcanvas). */
window.initCategoryForm = function (container) {
	container = container || document;
	var iconOptions = container.querySelectorAll('.icon-option');
	var selectedIconInput = container.querySelector('#selectedIcon');
	var colorHexInput = container.querySelector('#colorHex');
	var colorInput = container.querySelector('input[type="color"]');
	iconOptions.forEach(function (btn) {
		btn.addEventListener('click', function (e) {
			e.preventDefault();
			iconOptions.forEach(function (b) { b.classList.remove('active'); });
			this.classList.add('active');
			if (selectedIconInput) selectedIconInput.value = this.getAttribute('data-icon');
		});
	});
	if (colorInput && colorHexInput) {
		colorInput.addEventListener('input', function () { colorHexInput.value = this.value; });
		colorHexInput.addEventListener('input', function () {
			if (this.value.length === 7) colorInput.value = this.value;
		});
	}
};

/** Show a Bootstrap toast (non-blocking). Use instead of alert(). */
function showToast(message, type) {
	type = type || 'danger';
	var container = document.getElementById('toastContainer');
	if (!container) return;
	var id = 'toast-' + Date.now();
	var html = '<div class="toast align-items-center text-bg-' + type + ' border-0" role="alert" id="' + id + '">' +
		'<div class="d-flex"><div class="toast-body">' + (message || '') + '</div>' +
		'<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div></div>';
	container.insertAdjacentHTML('beforeend', html);
	var el = document.getElementById(id);
	if (el && typeof bootstrap !== 'undefined') {
		var t = new bootstrap.Toast(el, { autohide: true, delay: 5000 });
		t.show();
		el.addEventListener('hidden.bs.toast', function () { el.remove(); });
	}
}
