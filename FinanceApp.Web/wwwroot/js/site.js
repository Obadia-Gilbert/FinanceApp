// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
	// OFFCANVAS EDIT LOADER
	// When an element with `btn-edit` is clicked we fetch the Edit page
	// (GET) and load the response HTML into the right-side offcanvas.
	// This keeps users on the list page while editing.
	document.body.addEventListener('click', function (e) {
		const edit = e.target.closest('.btn-edit');
		if (!edit) return;
		e.preventDefault();
		const href = edit.getAttribute('href');
		const offcanvasEl = document.getElementById('offcanvasForm');
		const offcanvasBody = document.getElementById('offcanvasFormBody');

		// Fetch the edit form HTML and inject it into the offcanvas.
		// Add a query flag so server can return a layout-less partial, and include
		// the conventional "X-Requested-With" header so controllers can detect AJAX.
		let url = href;
		url += (href.indexOf('?') !== -1 ? '&' : '?') + 'partial=true';
		fetch(url, {
			credentials: 'same-origin',
			headers: { 'X-Requested-With': 'XMLHttpRequest' }
		})
			.then(resp => {
				if (!resp.ok) throw new Error('Network response was not ok');
				return resp.text();
			})
			.then(html => {
				// Replace offcanvas body with server-rendered form fragment
				offcanvasBody.innerHTML = html;
				// Show the offcanvas using Bootstrap's JS API
				const off = new bootstrap.Offcanvas(offcanvasEl);
				off.show();
			})
			.catch(err => {
				console.error('Failed to load form:', err);
				// Friendly error for users
				alert('Failed to load form. Please try again or refresh the page.');
			});
	});

	// DELETE NAVIGATION (direct navigation)
	// The app will navigate directly to the Delete confirmation page when
	// a `.btn-delete` link is clicked. We intentionally DO NOT intercept that
	// click here so the server-side Delete (GET->POST) flow remains intact.
});
