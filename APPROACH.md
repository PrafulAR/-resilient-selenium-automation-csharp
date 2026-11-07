# Short Approach Note

I treated this assignment as a semantic element-locating problem rather than a traditional selector-writing problem.

The main objective was to identify webpage elements using their user-facing meaning instead of depending on fixed HTML attributes such as IDs, names, and CSS classes.

### Implementation Approach

1. Search the current DOM for visible and enabled candidate controls.
2. Collect human-facing signals such as associated labels, ARIA attributes, placeholders, and accessible text.
3. Score candidate elements against the requested field meaning.
4. For radio buttons, evaluate both the individual option label and the surrounding radio-group context.
5. For the State field, support native HTML `<select>` elements and basic ARIA/custom combobox implementations.
6. Use explicit waiting to handle elements that appear after the initial page load.
7. Re-run semantic searches when necessary and reacquire elements if Selenium encounters stale element references.
8. Verify field interactions after entering or selecting values.

### Design Decisions

- Avoid relying on fixed IDs, names, or CSS classes.
- Prefer exact semantic matches when identifying fields with similar names.
- Use retry handling for stale element references.
- Keep the target URL and input values configurable.
- Report failures clearly when a required field or dropdown option cannot be found.

### Trade-offs and Limitations

Semantic matching is more flexible than fixed selectors, but it cannot reliably infer arbitrary changes in business meaning.

For example, if a page removes meaningful labels and accessibility information, identifying the intended field becomes difficult.

Custom widgets may also require additional handling depending on their implementation.

The solution therefore prioritizes reliable identification and explicit failure over silently interacting with an ambiguous or incorrect element.