/*
    Behaviour for the editor templates that need more than HTML can express:
    Tags, Color, Range and LineItem. Templates that need no script are not
    mentioned here at all.

    Dependency-free on purpose, and written against data- attributes rather than
    ids or classes, so a template can be restyled without breaking its script.
    Event delegation on document means markup added after page load works
    without re-wiring.
*/
(function () {
    'use strict';

    // --- Tags -------------------------------------------------------------
    // The visible text box is unnamed so it never posts. Each accepted tag adds
    // a hidden input carrying the field name, which the model binder collects
    // back into a List<string>.
    function addTag(editor, value) {
        var text = value.trim();
        if (!text) { return; }

        var name = editor.getAttribute('data-tags-name');
        var list = editor.querySelector('[data-tag-list]');

        var existing = Array.prototype.map.call(
            list.querySelectorAll('input[type="hidden"]'),
            function (input) { return input.value.toLowerCase(); });
        if (existing.indexOf(text.toLowerCase()) !== -1) { return; }

        var badge = document.createElement('span');
        badge.className = 'badge text-bg-primary d-inline-flex align-items-center gap-2 me-1 mb-1';

        var label = document.createElement('span');
        // textContent rather than innerHTML: a tag is user input.
        label.textContent = text;
        badge.appendChild(label);

        var remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'btn-close btn-close-white';
        remove.setAttribute('data-remove-tag', '');
        remove.setAttribute('aria-label', 'Remove tag');
        badge.appendChild(remove);

        var hidden = document.createElement('input');
        hidden.type = 'hidden';
        hidden.name = name;
        hidden.value = text;
        badge.appendChild(hidden);

        list.appendChild(badge);
    }

    document.addEventListener('keydown', function (event) {
        var input = event.target.closest && event.target.closest('[data-tag-input]');
        if (!input) { return; }

        if (event.key === 'Enter' || event.key === ',') {
            // Enter inside a form would otherwise submit it.
            event.preventDefault();
            addTag(input.closest('[data-tags-editor]'), input.value);
            input.value = '';
        } else if (event.key === 'Backspace' && input.value === '') {
            var badges = input.closest('[data-tags-editor]').querySelectorAll('[data-tag-list] .badge');
            if (badges.length) { badges[badges.length - 1].remove(); }
        }
    });

    // Capture phase: blur does not bubble.
    document.addEventListener('blur', function (event) {
        var input = event.target.closest && event.target.closest('[data-tag-input]');
        if (!input || !input.value.trim()) { return; }
        addTag(input.closest('[data-tags-editor]'), input.value);
        input.value = '';
    }, true);

    document.addEventListener('click', function (event) {
        var remove = event.target.closest && event.target.closest('[data-remove-tag]');
        if (remove) { remove.closest('.badge').remove(); }
    });

    // --- Colour -----------------------------------------------------------
    // Two inputs, one value: only the text box carries the field name, so
    // exactly one value posts back. Each keeps the other in step.
    document.addEventListener('input', function (event) {
        var target = event.target;
        if (!target.closest) { return; }

        var swatch = target.closest('[data-color-text]');
        if (swatch) {
            var text = document.getElementById(swatch.getAttribute('data-color-text'));
            if (text) { text.value = swatch.value; }
            return;
        }

        var box = target.closest('[data-color-swatch]');
        if (box && /^#[0-9a-f]{6}$/i.test(box.value)) {
            var picker = document.getElementById(box.getAttribute('data-color-swatch'));
            if (picker) { picker.value = box.value; }
        }
    });

    // --- Range ------------------------------------------------------------
    document.addEventListener('input', function (event) {
        var slider = event.target.closest && event.target.closest('[data-range-output]');
        if (!slider) { return; }
        var output = document.getElementById(slider.getAttribute('data-range-output'));
        if (output) { output.textContent = slider.value; }
    });

    // --- Line items -------------------------------------------------------
    // Removing a row leaves a gap in the posted indexes (0, 2, 3). The default
    // model binder stops at the first missing index, so rows are renumbered
    // after each removal. Without this, deleting the second of four rows
    // silently discards the last two.
    function renumber(container) {
        var rows = container.querySelectorAll('[data-line-item]');
        Array.prototype.forEach.call(rows, function (row, index) {
            Array.prototype.forEach.call(row.querySelectorAll('[name]'), function (field) {
                field.name = field.name.replace(/\[\d+\]/, '[' + index + ']');
            });
            Array.prototype.forEach.call(row.querySelectorAll('[id]'), function (field) {
                field.id = field.id.replace(/_\d+__/, '_' + index + '__');
            });
            Array.prototype.forEach.call(row.querySelectorAll('label[for]'), function (label) {
                label.htmlFor = label.htmlFor.replace(/_\d+__/, '_' + index + '__');
            });
        });
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest && event.target.closest('[data-remove-line-item]');
        if (!button) { return; }

        var row = button.closest('[data-line-item]');
        var container = row.parentElement;
        row.remove();
        renumber(container);
    });
})();
