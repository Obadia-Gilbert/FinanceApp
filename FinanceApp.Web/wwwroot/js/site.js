// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', function () {
	// Edit/delete flows are handled by page-specific scripts (Expense, Category, Transaction).
});

// ============================================================
// Supporting Documents — document-level event delegation
// Works for both static HTML and content injected via innerHTML.
// ============================================================

/**
 * After a successful upload or delete, reload the offcanvas content
 * by fetching the edit partial from ReloadUrl, or fall back to a full
 * page reload if no ReloadUrl is available.
 */
function reloadDocsContainer(triggerElement) {
	var widget = triggerElement.closest('[data-reload-url]');
	var reloadUrl = widget ? widget.dataset.reloadUrl : null;
	var container = triggerElement.closest('[id$="OffcanvasContent"]');

	if (!reloadUrl || !container) {
		window.location.reload();
		return;
	}

	fetch(reloadUrl, {
		credentials: 'include',
		headers: { 'X-Requested-With': 'XMLHttpRequest' }
	})
	.then(function (res) { return res.ok ? res.text() : null; })
	.then(function (html) {
		if (html) {
			container.innerHTML = html;
			if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
				jQuery.validator.unobtrusive.parse(container);
			}
		}
	});
}

// 1. Preview button — opens Bootstrap modal with image or PDF iframe
document.addEventListener('click', function (e) {
	var btn = e.target.closest('.btn-preview-doc');
	if (!btn) return;

	var url         = btn.dataset.previewUrl;
	var modalId     = btn.dataset.modalId;
	var contentType = btn.dataset.contentType;
	if (!url || !modalId) return;

	var modalEl   = document.getElementById(modalId);
	var contentEl = document.getElementById(modalId + 'Content');
	if (!modalEl) return;

	if (contentEl) {
		contentEl.innerHTML = '<div class="d-flex align-items-center justify-content-center py-5">'
			+ '<span class="spinner-border text-light" role="status"></span></div>';
	}

	var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
	modal.show();

	if (!contentEl) return;

	if (contentType && contentType.startsWith('image/')) {
		var img = new Image();
		img.onload  = function () {
			contentEl.innerHTML = '<img src="' + url + '" alt="Preview"'
				+ ' style="max-width:100%;max-height:70vh;object-fit:contain;" />';
		};
		img.onerror = function () {
			contentEl.innerHTML = '<p class="text-light py-4">Unable to load image.</p>';
		};
		img.src = url;
	} else {
		contentEl.innerHTML = '<iframe src="' + url + '" style="width:100%;height:70vh;border:none;"'
			+ ' title="Document Preview"></iframe>';
	}
});

// 2. Upload form — AJAX submit, reload container on success
document.addEventListener('submit', function (e) {
	var form = e.target;
	if (!form.hasAttribute('data-docs-upload-form')) return;
	e.preventDefault();

	var formData  = new FormData(form);
	var submitBtn = form.querySelector('.docs-upload-btn');
	var label     = submitBtn ? submitBtn.querySelector('.docs-upload-label') : null;
	if (submitBtn) { submitBtn.disabled = true; }
	if (label) { label.textContent = 'Uploading\u2026'; }

	fetch(form.action, {
		method: 'POST',
		credentials: 'include',
		headers: { 'X-Requested-With': 'XMLHttpRequest' },
		body: formData
	})
	.then(function (res) { return res.json(); })
	.then(function (json) {
		if (json.success) {
			showToast(json.message || 'Document uploaded.', 'success');
			reloadDocsContainer(form);
		} else {
			showToast(json.message || 'Upload failed.', 'danger');
			if (submitBtn) { submitBtn.disabled = false; }
			if (label) { label.textContent = 'Upload'; }
		}
	})
	.catch(function () {
		showToast('Upload failed. Please try again.', 'danger');
		if (submitBtn) { submitBtn.disabled = false; }
		if (label) { label.textContent = 'Upload'; }
	});
});

// 3. Delete form — AJAX submit with confirmation, reload container on success
document.addEventListener('submit', function (e) {
	var form = e.target;
	if (!form.hasAttribute('data-docs-delete-form')) return;
	if (!confirm('Delete this document? This cannot be undone.')) {
		e.preventDefault();
		return;
	}
	e.preventDefault();

	fetch(form.action, {
		method: 'POST',
		credentials: 'include',
		headers: { 'X-Requested-With': 'XMLHttpRequest' },
		body: new FormData(form)
	})
	.then(function (res) { return res.json(); })
	.then(function (json) {
		if (json.success) {
			showToast(json.message || 'Document deleted.', 'success');
			reloadDocsContainer(form);
		} else {
			showToast(json.message || 'Delete failed.', 'danger');
		}
	})
	.catch(function () {
		showToast('Delete failed. Please try again.', 'danger');
	});
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
