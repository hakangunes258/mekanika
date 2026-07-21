// ============================================================
// pdfGenerator.js  –  Mekanika PDF Export  (v7 – autotable)
// ============================================================

// ---------- SVG → Base64 helper (used by MomentOfInertia) ----------
window.convertSvgToBase64 = function (svgString) {
    return new Promise((resolve) => {
        try {
            const svgBlob = new Blob([svgString], { type: 'image/svg+xml;charset=utf-8' });
            const url = URL.createObjectURL(svgBlob);
            const img = new Image();
            img.onload = function () {
                const canvas = document.createElement('canvas');
                canvas.width  = (img.width  || 300) * 2;
                canvas.height = (img.height || 200) * 2;
                const ctx = canvas.getContext('2d');
                ctx.fillStyle = '#fafafa';
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.scale(2, 2);
                ctx.drawImage(img, 0, 0);
                URL.revokeObjectURL(url);
                resolve(canvas.toDataURL('image/png'));
            };
            img.onerror = () => { URL.revokeObjectURL(url); resolve(''); };
            img.src = url;
        } catch (e) { resolve(''); }
    });
};

// ---------- Logo loader ----------
function getLogoBase64() {
    return new Promise((resolve) => {
        const img = new Image();
        img.crossOrigin = 'Anonymous';
        img.src = window.location.origin + '/logo.png';
        img.onload = () => {
            try {
                const c = document.createElement('canvas');
                c.width = img.naturalWidth; c.height = img.naturalHeight;
                c.getContext('2d').drawImage(img, 0, 0);
                resolve(c.toDataURL('image/png'));
            } catch (e) { resolve(null); }
        };
        img.onerror = () => resolve(null);
        setTimeout(() => { if (!img.complete) resolve(null); }, 2000);
    });
}

// ---------- Character-cleaning helper ----------
// Replaces every Unicode character that jsPDF/Helvetica cannot encode
// with a safe ASCII/Latin-1 equivalent.
function cleanText(raw) {
    if (!raw) return '';
    return raw
        // Greek / math symbols used in engineering
        .replace(/τ/g, 'tau')
        .replace(/σ/g, 'sigma')
        .replace(/λ/g, 'lambda')
        .replace(/μ/g, 'mu')
        .replace(/π/g, 'pi')
        .replace(/φ/g, 'phi')
        .replace(/α/g, 'alpha')
        .replace(/β/g, 'beta')
        .replace(/γ/g, 'gamma')
        .replace(/δ/g, 'delta')
        .replace(/ε/g, 'epsilon')
        .replace(/η/g, 'eta')
        .replace(/θ/g, 'theta')
        .replace(/ρ/g, 'rho')
        .replace(/ω/g, 'omega')
        .replace(/Σ/g, 'Sigma')
        .replace(/Δ/g, 'Delta')
        // Subscript/superscript digits (Unicode blocks U+2080–U+2089, U+00B2 etc.)
        .replace(/₀/g, '0').replace(/₁/g, '1').replace(/₂/g, '2')
        .replace(/₃/g, '3').replace(/₄/g, '4').replace(/₅/g, '5')
        .replace(/₆/g, '6').replace(/₇/g, '7').replace(/₈/g, '8').replace(/₉/g, '9')
        .replace(/⁰/g, '0').replace(/¹/g, '1').replace(/²/g, '2')
        .replace(/³/g, '3').replace(/⁴/g, '4').replace(/⁵/g, '5')
        .replace(/⁶/g, '6').replace(/⁷/g, '7').replace(/⁸/g, '8').replace(/⁹/g, '9')
        // Status icons — handle "✓ OK", "❌ FAIL", "⚠️ Marginal" patterns
        .replace(/✓\s*OK/g,       'OK')
        .replace(/✔\s*OK/g,       'OK')
        .replace(/❌\s*FAIL/g,    'FAIL')
        .replace(/⚠️\s*Marginal/g, 'Marginal')
        .replace(/⚠\s*Marginal/g,  'Marginal')
        .replace(/⚠️\s*Note:/g,   'Note:')
        .replace(/⚠️\s*Warning:/g,'Warning:')
        .replace(/✓\s*/g,  'OK ')
        .replace(/✔\s*/g,  'OK ')
        .replace(/❌\s*/g, 'FAIL ')
        .replace(/⚠️/g,   'WARN ')
        .replace(/⚠/g,    'WARN ')
        .replace(/🔒/g, '')
        .replace(/⚡/g, '')
        // Math / special
        .replace(/∞/g,  'Inf')
        .replace(/≥/g,  '>=')
        .replace(/≤/g,  '<=')
        .replace(/≠/g,  '!=')
        .replace(/×/g,  'x')
        .replace(/÷/g,  '/')
        .replace(/°/g,  'deg')
        .replace(/±/g,  '+/-')
        .replace(/·/g,  '.')
        // Arrows
        .replace(/→/g, '->')
        .replace(/←/g, '<-')
        .replace(/↑/g, '^')
        .replace(/↓/g, 'v')
        // Dashes / quotes
        .replace(/–/g, '-').replace(/—/g, '-')
        .replace(/\u2018/g, "'").replace(/\u2019/g, "'")
        .replace(/\u201C/g, '"').replace(/\u201D/g, '"')
        // Bullet / misc
        .replace(/•/g, '-')
        .replace(/…/g, '...')
        // Remove any remaining non-Latin-1 chars (codepoint > 255)
        .replace(/[^\x00-\xFF]/g, '?')
        .trim();
}

// Get clean text from a DOM element,
// optionally looking for a data-pdf-text override attribute.
function cellText(el) {
    if (!el) return '';
    // If the Razor template set a clean override, use it
    const override = el.getAttribute('data-pdf-text');
    if (override !== null) return cleanText(override);
    return cleanText(el.textContent);
}

// Row background colour from CSS class
function rowFill(row) {
    if (row.classList.contains('danger'))    return [255, 235, 238];
    if (row.classList.contains('warning'))   return [255, 248, 225];
    if (row.classList.contains('success'))   return [232, 245, 233];
    if (row.classList.contains('highlight')) return [227, 242, 253];
    return null; // default white
}

// ============================================================
// Main entry point called by Blazor:
//   generatePdf(elementId, fileName)
// ============================================================
window.generatePdf = async function (elementId, fileName) {
    console.log(`[PDF v7] Starting for #${elementId}`);

    const element = document.getElementById(elementId);
    if (!element) { alert('Error: Could not find content element.'); return; }

    // Loading overlay
    const loadingDiv = document.createElement('div');
    loadingDiv.id = 'pdf-loading';
    loadingDiv.style.cssText =
        'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.6);' +
        'display:flex;justify-content:center;align-items:center;z-index:9999;';
    loadingDiv.innerHTML =
        '<div style="background:white;padding:40px 60px;border-radius:12px;text-align:center;box-shadow:0 4px 20px rgba(0,0,0,0.3);">' +
        '<p style="margin:0 0 10px 0;font-size:18px;font-weight:600;color:#333;">Generating PDF</p>' +
        '<p style="margin:0;font-size:14px;color:#666;">Please wait...</p></div>';
    document.body.appendChild(loadingDiv);

    try {
        const { jsPDF } = window.jspdf;
        const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });

        const pageW  = doc.internal.pageSize.getWidth();
        const pageH  = doc.internal.pageSize.getHeight();
        const margin = 14;
        const cW     = pageW - 2 * margin;  // content width
        let   curY   = 32;

        const logoBase64 = await getLogoBase64();

        // ---- Header (called once per page) ----
        function addHeader() {
            if (logoBase64) {
                try { doc.addImage(logoBase64, 'PNG', margin, 7, 32, 0); }
                catch (e) { fallbackHeader(); }
            } else { fallbackHeader(); }

            function fallbackHeader() {
                doc.setFontSize(15);
                doc.setTextColor(102, 126, 234);
                doc.setFont('helvetica', 'bold');
                doc.text('MEKANIKA', margin, 15);
            }

            doc.setFontSize(10);
            doc.setTextColor(120, 120, 120);
            doc.setFont('helvetica', 'normal');
            doc.text('Calculation Report', pageW - margin, 15, { align: 'right' });

            doc.setDrawColor(200, 200, 200);
            doc.setLineWidth(0.4);
            doc.line(margin, 22, pageW - margin, 22);
        }

        // ---- Footer ----
        function addFooter(pageNum, totalPages) {
            doc.setFontSize(8);
            doc.setTextColor(160, 160, 160);
            doc.setFont('helvetica', 'normal');
            doc.text(
                `Generated by Mekanika (www.mekanika.org) | Page ${pageNum} of ${totalPages}`,
                pageW / 2, pageH - 8, { align: 'center' }
            );
        }

        // ---- Page-break guard ----
        function checkBreak(needed) {
            if (curY + needed > pageH - 22) {
                doc.addPage();
                curY = 32;
                addHeader();
                return true;
            }
            return false;
        }

        addHeader();

        // ---- Process each .card ----
        const cards = element.querySelectorAll('.card');
        console.log(`[PDF v7] Found ${cards.length} cards`);

        cards.forEach((card, ci) => {
            // Card section title (blue bar)
            const headerEl  = card.querySelector('.card-header h2');
            const headerTxt = headerEl ? cleanText(headerEl.textContent) : `Section ${ci + 1}`;

            checkBreak(18);
            doc.setFillColor(102, 126, 234);
            doc.roundedRect(margin, curY, cW, 9, 1.5, 1.5, 'F');
            doc.setFontSize(11);
            doc.setTextColor(255, 255, 255);
            doc.setFont('helvetica', 'bold');
            doc.text(headerTxt, margin + 4, curY + 6.2);
            curY += 12;

            // ---- Tables via autoTable ----
            const tables = card.querySelectorAll('table');
            tables.forEach((table) => {
                // Build rows array
                const body = [];
                table.querySelectorAll('tbody tr').forEach((row) => {
                    const tds   = row.querySelectorAll('td');
                    const count = tds.length;
                    const fill  = rowFill(row);

                    // Determine column content
                    let rowData;
                    if (count >= 4) {
                        rowData = [
                            cellText(tds[0]),
                            cellText(tds[1]),
                            cellText(tds[2]),
                            cellText(tds[3])
                        ];
                    } else if (count === 3) {
                        rowData = [cellText(tds[0]), '', cellText(tds[1]), cellText(tds[2])];
                    } else if (count === 2) {
                        rowData = [cellText(tds[0]), '', cellText(tds[1]), ''];
                    } else {
                        rowData = [cellText(tds[0]), '', '', ''];
                    }

                    const isBold = row.classList.contains('highlight') ||
                                   row.classList.contains('danger')    ||
                                   row.classList.contains('warning')   ||
                                   row.classList.contains('success');

                    // Build cell style overrides
                    const cellStyles = {};
                    if (fill) {
                        for (let i = 0; i < 4; i++) {
                            cellStyles[i] = { fillColor: fill };
                        }
                    }

                    body.push({
                        cells:      rowData,
                        isBold:     isBold,
                        cellStyles: cellStyles
                    });
                });

                if (body.length === 0) return;

                // Column widths: Description | Symbol | Value | Unit
                const colWidths = [cW * 0.44, cW * 0.16, cW * 0.24, cW * 0.16];

                doc.autoTable({
                    startY:    curY,
                    margin:    { left: margin, right: margin },
                    tableWidth: cW,
                    head: [[
                        { content: 'Description', styles: { halign: 'left'   } },
                        { content: 'Symbol',      styles: { halign: 'center' } },
                        { content: 'Value',       styles: { halign: 'right'  } },
                        { content: 'Unit',        styles: { halign: 'center' } }
                    ]],
                    body: body.map(r => r.cells),
                    columnStyles: {
                        0: { cellWidth: colWidths[0], halign: 'left',   fontStyle: 'normal' },
                        1: { cellWidth: colWidths[1], halign: 'center', fontStyle: 'italic', textColor: [100, 100, 100] },
                        2: { cellWidth: colWidths[2], halign: 'right',  fontStyle: 'normal' },
                        3: { cellWidth: colWidths[3], halign: 'center', textColor: [100, 100, 100] }
                    },
                    headStyles: {
                        fillColor:  [248, 249, 250],
                        textColor:  [44,  62,  80],
                        fontStyle:  'bold',
                        fontSize:   9,
                        lineWidth:  0,
                        lineColor:  [222, 226, 230]
                    },
                    bodyStyles: {
                        fontSize:   9,
                        cellPadding: { top: 2.2, bottom: 2.2, left: 3, right: 3 },
                        lineColor:  [238, 238, 238],
                        lineWidth:  0.2,
                        textColor:  [50, 50, 50]
                    },
                    alternateRowStyles: { fillColor: [255, 255, 255] },
                    // Per-row styling via willDrawCell
                    willDrawCell: (data) => {
                        if (data.section !== 'body') return;
                        const rowMeta = body[data.row.index];
                        if (!rowMeta) return;
                        const fill = rowMeta.cellStyles[data.column.index];
                        if (fill && fill.fillColor) {
                            data.cell.styles.fillColor = fill.fillColor;
                        }
                        if (rowMeta.isBold) {
                            data.cell.styles.fontStyle = 'bold';
                        }
                    },
                    didDrawPage: (data) => {
                        // Re-draw header on new page
                        if (data.pageNumber > 1) addHeader();
                    }
                });

                curY = doc.lastAutoTable.finalY + 4;
            });

            // ---- Alert boxes ----
            card.querySelectorAll('.alert').forEach((alert) => {
                checkBreak(14);
                const txt = cleanText(alert.textContent);
                let bg, fg;
                if (alert.classList.contains('alert-danger')) {
                    bg = [248, 215, 218]; fg = [114, 28, 36];
                } else if (alert.classList.contains('alert-warning')) {
                    bg = [255, 243, 205]; fg = [133, 100, 4];
                } else if (alert.classList.contains('alert-success')) {
                    bg = [212, 237, 218]; fg = [21, 87, 36];
                } else {
                    bg = [209, 236, 241]; fg = [12, 84, 96];
                }
                doc.setFillColor(...bg);
                doc.roundedRect(margin, curY, cW, 10, 1.5, 1.5, 'F');
                doc.setFontSize(9);
                doc.setTextColor(...fg);
                doc.setFont('helvetica', 'bold');
                // Truncate to page width
                let alertLine = txt;
                while (doc.getTextWidth(alertLine) > cW - 10 && alertLine.length > 4) {
                    alertLine = alertLine.slice(0, -1);
                }
                doc.text(alertLine, margin + 5, curY + 6.5);
                curY += 13;
            });

            // ---- Inline images (base64, e.g. MomentOfInertia SVG) ----
            card.querySelectorAll('img[src^="data:image"]').forEach((img) => {
                checkBreak(65);
                try {
                    const iW = 90;
                    const iX = (pageW - iW) / 2;
                    doc.addImage(img.src, 'PNG', iX, curY, iW, 0);
                    curY += 58;
                } catch (e) { console.warn('[PDF v7] Image error:', e); }
            });

            curY += 5; // gap between cards
        });

        // If element had no .card children, do a plain text dump
        if (cards.length === 0) {
            element.querySelectorAll('p, h2, h3, h4').forEach((el) => {
                checkBreak(9);
                const isH = el.tagName.startsWith('H');
                doc.setFontSize(isH ? 12 : 9);
                doc.setFont('helvetica', isH ? 'bold' : 'normal');
                doc.setTextColor(50, 50, 50);
                doc.text(cleanText(el.textContent), margin + 3, curY);
                curY += isH ? 9 : 7;
            });
        }

        // ---- Stamp footers on all pages ----
        const total = doc.internal.getNumberOfPages();
        for (let i = 1; i <= total; i++) {
            doc.setPage(i);
            if (i > 1) addHeader();   // header already on page 1
            addFooter(i, total);
        }

        doc.save(fileName);
        console.log('[PDF v7] Done.');

    } catch (err) {
        console.error('[PDF v7] Error:', err);
        alert('PDF generation error: ' + err.message);
    } finally {
        const ld = document.getElementById('pdf-loading');
        if (ld) ld.remove();
    }
};
