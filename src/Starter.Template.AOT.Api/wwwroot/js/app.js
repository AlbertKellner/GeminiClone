// Disk Explorer — main application
// Adapted from GeminiClone _Layout.cshtml inline JS
// API calls adapted from /api/Structure/* to /drives/* (Starter.Template.AOT endpoints)

let myChart = null;

// ─── Initialization ──────────────────────────────────────────────────────────

document.addEventListener("DOMContentLoaded", fetchDrives);

// ─── Drive list ──────────────────────────────────────────────────────────────

function fetchDrives() {
    setStatus("Carregando drives...");

    fetch("/drives")
        .then(r => {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        })
        .then(data => {
            const dropdown = document.getElementById("getDisks");
            dropdown.innerHTML = "";

            const emptyOpt = document.createElement("option");
            emptyOpt.value = "";
            emptyOpt.text = "Selecione um drive...";
            dropdown.add(emptyOpt);

            data.drives.forEach(drive => {
                const opt = document.createElement("option");
                opt.value = drive.id;
                opt.text = drive.name + "  (" + drive.formattedTotalSize + " total, " + drive.formattedAvailableSize + " livre)";
                dropdown.add(opt);
            });

            setStatus(data.drives.length + " drive(s) encontrado(s)");
        })
        .catch(err => {
            setStatus("Erro ao carregar drives: " + err.message);
            console.error(err);
        });
}

// ─── Load drive structure ─────────────────────────────────────────────────────

function getStructure(driveId) {
    if (!driveId) return;

    document.querySelector(".chart").innerHTML = "";
    document.querySelector("#FolderItems tbody").innerHTML = "";
    setStatus("Carregando estrutura de " + driveId + "...");

    fetch("/drives/" + driveId + "/items")
        .then(r => {
            if (!r.ok) throw new Error("HTTP " + r.status);
            return r.json();
        })
        .then(data => {
            if (!data.root) {
                setStatus("Nenhum dado retornado para " + driveId);
                return;
            }

            const sunburstData = transformNode(data.root);

            const totalDepth = countDescendants(sunburstData.children || []);
            if (sunburstData.children && sunburstData.children.length > 0) {
                sunburstData.children.forEach((child, i) => {
                    addColorToChildren(child, generateBaseColor(i), totalDepth);
                });
            }

            generateGraph(sunburstData);
            setStatus("Drive " + driveId + " carregado");
        })
        .catch(err => {
            setStatus("Erro: " + err.message);
            document.querySelector(".chart").innerHTML =
                '<div style="color:#f38ba8;padding:24px;font-size:14px;">Erro ao carregar: ' + err.message + '</div>';
            console.error(err);
        });
}

// ─── Transform API response → sunburst format ────────────────────────────────

function transformNode(node) {
    return {
        name: node.name,
        value: node.sizeBytes,
        formattedSize: node.formattedSize,
        children: (node.children || []).map(transformNode)
    };
}

// ─── Render sunburst chart ────────────────────────────────────────────────────

function generateGraph(data) {
    myChart = Sunburst();

    document.querySelector(".chart").innerHTML = "";

    removeValueIfChildren(data);

    myChart
        .data(data)
        .label("name")
        .size("value")
        .minSliceAngle(0.4)
        .excludeRoot(true)
        .showLabels(true)
        .labelOrientation("angular")
        .onHover((node) => {
            if (node) console.log("[DiskExplorer] hover:", node.name);
        })
        .onClick((node) => {
            myChart.focusOnNode(node);
            if (node && node.children) {
                displayFolderItems(node);
            }
        })
        .color("color")
        .tooltipContent((d, node) => "Tamanho: <i>" + (d.formattedSize || node.value) + "</i>")
        (document.getElementById("sunburstChart"));

    displayFolderItems(data);
}

// ─── Populate folder items table ─────────────────────────────────────────────

function displayFolderItems(node) {
    const tbody = document.querySelector("#FolderItems tbody");
    tbody.innerHTML = "";

    if (!node || !node.children) return;

    node.children.forEach(item => {
        const tr = document.createElement("tr");

        const tdName = document.createElement("td");
        tdName.textContent = item.name;
        tdName.title = item.name;
        tr.appendChild(tdName);

        const tdSize = document.createElement("td");
        tdSize.textContent = item.formattedSize || "";
        tr.appendChild(tdSize);

        tbody.appendChild(tr);
    });
}

// ─── Sunburst helper: remove value from parent nodes ────────────────────────
// Parent sizes are computed by the chart from children values.

function removeValueIfChildren(node) {
    if (node.children && node.children.length > 0) {
        delete node.value;
        node.children.forEach(removeValueIfChildren);
    }
}

// ─── Status bar ──────────────────────────────────────────────────────────────

function setStatus(msg) {
    const el = document.getElementById("status-msg");
    if (el) el.textContent = msg;
}
