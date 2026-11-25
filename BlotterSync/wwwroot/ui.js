/**
 * UI Helper Functions
 * Common utilities for the BlotterSync standalone app
 */

class UIHelpers {
  /**
   * Show a modal dialog
   */
  static showModal(title, content, actions = []) {
    const modal = document.createElement("div");
    modal.className = "modal";
    modal.innerHTML = `
      <div class="modal-backdrop"></div>
      <div class="modal-content">
        <div class="modal-header">
          <h2>${title}</h2>
          <button class="modal-close">&times;</button>
        </div>
        <div class="modal-body">
          ${content}
        </div>
        <div class="modal-footer">
          ${actions
            .map(
              (a) =>
                `<button class="button ${a.style || "secondary"}" data-action="${a.id}">${a.label}</button>`
            )
            .join("")}
        </div>
      </div>
    `;

    document.body.appendChild(modal);

    const closeBtn = modal.querySelector(".modal-close");
    const backdrop = modal.querySelector(".modal-backdrop");

    closeBtn.addEventListener("click", () => modal.remove());
    backdrop.addEventListener("click", () => modal.remove());

    modal.querySelectorAll("[data-action]").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        const action = actions.find((a) => a.id === btn.dataset.action);
        if (action && action.callback) {
          action.callback();
        }
        modal.remove();
      });
    });

    return modal;
  }

  /**
   * Format table data for display
   */
  static formatTable(data, columns) {
    const headers = columns.map((c) => `<th>${c.label}</th>`).join("");
    const rows = data
      .map(
        (row) =>
          `<tr>${columns.map((c) => `<td>${c.render ? c.render(row[c.key]) : row[c.key]}</td>`).join("")}</tr>`
      )
      .join("");

    return `<table><thead><tr>${headers}</tr></thead><tbody>${rows}</tbody></table>`;
  }

  /**
   * Create a form field
   */
  static createFormField(type, name, label, options = {}) {
    const id = `field-${name}`;
    let input = "";

    switch (type) {
      case "text":
      case "email":
      case "password":
      case "date":
      case "time":
      case "number":
      case "search":
        input = `<input type="${type}" id="${id}" name="${name}" ${this._buildAttrs(options)}>`;
        break;
      case "textarea":
        input = `<textarea id="${id}" name="${name}" ${this._buildAttrs(options)}></textarea>`;
        break;
      case "select":
        const opts = (options.options || [])
          .map((o) => `<option value="${o.value}">${o.label}</option>`)
          .join("");
        input = `<select id="${id}" name="${name}" ${this._buildAttrs(options)}><option value="">Select...</option>${opts}</select>`;
        break;
      case "checkbox":
        input = `<input type="checkbox" id="${id}" name="${name}" ${this._buildAttrs(options)}>`;
        break;
      case "radio":
        const radios = (options.options || [])
          .map(
            (o) =>
              `<label><input type="radio" name="${name}" value="${o.value}" ${this._buildAttrs(options)}> ${o.label}</label>`
          )
          .join("");
        input = `<div class="radio-group">${radios}</div>`;
        break;
    }

    return `
      <div class="form-field">
        <label for="${id}">${label}</label>
        ${input}
        ${options.help ? `<small>${options.help}</small>` : ""}
      </div>
    `;
  }

  /**
   * Build HTML attributes from options
   */
  static _buildAttrs(options) {
    const attrs = [];
    if (options.required) attrs.push("required");
    if (options.disabled) attrs.push("disabled");
    if (options.readonly) attrs.push("readonly");
    if (options.placeholder) attrs.push(`placeholder="${options.placeholder}"`);
    if (options.maxlength) attrs.push(`maxlength="${options.maxlength}"`);
    if (options.minlength) attrs.push(`minlength="${options.minlength}"`);
    if (options.min) attrs.push(`min="${options.min}"`);
    if (options.max) attrs.push(`max="${options.max}"`);
    if (options.step) attrs.push(`step="${options.step}"`);
    if (options.rows) attrs.push(`rows="${options.rows}"`);
    if (options.cols) attrs.push(`cols="${options.cols}"`);
    return attrs.join(" ");
  }

  /**
   * Show a confirmation dialog
   */
  static confirm(message) {
    return new Promise((resolve) => {
      this.showModal("Confirm", message, [
        {
          id: "confirm",
          label: "Confirm",
          style: "primary",
          callback: () => resolve(true),
        },
        {
          id: "cancel",
          label: "Cancel",
          style: "secondary",
          callback: () => resolve(false),
        },
      ]);
    });
  }

  /**
   * Show an alert dialog
   */
  static alert(message) {
    return new Promise((resolve) => {
      this.showModal("Alert", message, [
        {
          id: "ok",
          label: "OK",
          style: "primary",
          callback: resolve,
        },
      ]);
    });
  }

  /**
   * Format a date for display
   */
  static formatDate(date, format = "short") {
    const d = new Date(date);
    if (format === "short") {
      return d.toLocaleDateString("en-PH");
    } else if (format === "long") {
      return d.toLocaleDateString("en-PH", {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
    } else if (format === "time") {
      return d.toLocaleTimeString("en-PH");
    }
    return d.toLocaleString("en-PH");
  }

  /**
   * Truncate text
   */
  static truncate(text, length = 50) {
    if (!text) return "";
    return text.length > length ? text.substring(0, length) + "…" : text;
  }

  /**
   * Debounce a function
   */
  static debounce(func, wait = 300) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }

  /**
   * Throttle a function
   */
  static throttle(func, limit = 300) {
    let inThrottle;
    return function (...args) {
      if (!inThrottle) {
        func.apply(this, args);
        inThrottle = true;
        setTimeout(() => (inThrottle = false), limit);
      }
    };
  }

  /**
   * HTML escape
   */
  static escape(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Generate initials from name
   */
  static getInitials(name, length = 2) {
    return name
      .split(" ")
      .map((n) => n[0])
      .slice(0, length)
      .join("")
      .toUpperCase();
  }

  /**
   * Color code by status
   */
  static getStatusColor(status) {
    const colors = {
      pending: "#f59e0b",
      ongoing: "#3b82f6",
      resolved: "#10b981",
      dismissed: "#6b7280",
      needs_review: "#8b5cf6",
      accepted: "#06b6d4",
      rejected: "#ef4444",
    };
    return colors[status?.toLowerCase()] || "#6b7280";
  }

  /**
   * Load and cache images
   */
  static lazyLoadImages() {
    if ("IntersectionObserver" in window) {
      const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const img = entry.target;
            img.src = img.dataset.src;
            img.classList.add("loaded");
            observer.unobserve(img);
          }
        });
      });

      document.querySelectorAll("img[data-src]").forEach((img) => imageObserver.observe(img));
    }
  }

  /**
   * Copy to clipboard
   */
  static copyToClipboard(text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      return navigator.clipboard.writeText(text).then(() => true).catch(() => false);
    }

    // Fallback for older browsers
    const textArea = document.createElement("textarea");
    textArea.value = text;
    document.body.appendChild(textArea);
    textArea.select();
    try {
      document.execCommand("copy");
      document.body.removeChild(textArea);
      return Promise.resolve(true);
    } catch (err) {
      document.body.removeChild(textArea);
      return Promise.resolve(false);
    }
  }

  /**
   * Download a file
   */
  static downloadFile(content, filename, type = "text/plain") {
    const blob = new Blob([content], { type });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  /**
   * Parse URL parameters
   */
  static getQueryParams() {
    const params = {};
    new URLSearchParams(window.location.search).forEach((value, key) => {
      params[key] = value;
    });
    return params;
  }

  /**
   * Check if element is in viewport
   */
  static isInViewport(element) {
    const rect = element.getBoundingClientRect();
    return (
      rect.top >= 0 &&
      rect.left >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
      rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  }

  /**
   * Smooth scroll to element
   */
  static smoothScroll(target) {
    const element = typeof target === "string" ? document.querySelector(target) : target;
    if (element) {
      element.scrollIntoView({ behavior: "smooth" });
    }
  }

  /**
   * Animate element
   */
  static animate(element, animation, duration = 300) {
    return new Promise((resolve) => {
      element.style.animation = `${animation} ${duration}ms ease-in-out`;
      setTimeout(() => {
        element.style.animation = "";
        resolve();
      }, duration);
    });
  }

  /**
   * Check if device is mobile
   */
  static isMobile() {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  }

  /**
   * Get element dimensions
   */
  static getDimensions(element) {
    const rect = element.getBoundingClientRect();
    return {
      width: rect.width,
      height: rect.height,
      top: rect.top,
      left: rect.left,
      right: rect.right,
      bottom: rect.bottom,
    };
  }

  /**
   * Create a toast notification
   */
  static toast(message, duration = 3000) {
    const toast = document.createElement("div");
    toast.className = "toast-notification";
    toast.textContent = message;
    document.body.appendChild(toast);

    setTimeout(() => {
      toast.classList.add("show");
    }, 10);

    setTimeout(() => {
      toast.classList.remove("show");
      setTimeout(() => document.body.removeChild(toast), 300);
    }, duration);
  }
}

// Export for use in other modules
if (typeof module !== "undefined" && module.exports) {
  module.exports = UIHelpers;
}
