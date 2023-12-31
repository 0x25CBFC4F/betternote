function getPureUrl() {
    return window.location.href.split('#')[0];
}

// Stolen from https://stackoverflow.com/a/72239825/12030195
async function copyToClipboard(text) {
    if (navigator.clipboard) {
        // If normal copy method available, use it
        await navigator.clipboard.writeText(text);
        return;
    }

    const textArea = document.createElement("textarea");
    textArea.value = text;
    document.body.appendChild(textArea);
    textArea.focus({ preventScroll: true });
    textArea.select();
    try {
        // noinspection JSDeprecatedSymbols
        document.execCommand('copy');
    } catch (err) {
        console.error('Unable to copy to clipboard', err);
    }
    document.body.removeChild(textArea);
}

window.onload = async () => {
    const textArea = document.getElementById("textArea");
    const generateButton = document.getElementById("createLinkBtn");
    const resetButton = document.getElementById("resetBtn");

    adjustHeight(textArea);
    textArea.onkeyup = _ => adjustHeight(textArea);

    generateButton.onclick = onButtonClick;

    resetButton.onclick = async _ => {
        // Lazy but like who cares? :)
        await copyToClipboard(textArea.value);
        document.location.replace(getPureUrl());
    };
    
    if (document.location.hash.startsWith("#v"))
        await loadSecret(); // Time to decrypt!
};

async function loadSecret() {
    const textArea = document.getElementById("textArea");
    let docHash = document.location.hash;
    
    if(docHash.indexOf("%23") !== -1) {
        // It seems that link was escaped by some messenger
        docHash = decodeURIComponent(docHash);
    }
    
    const hash = docHash.split("#");
    
    if(hash.length !== 5) {
        showAlert("danger", "Invalid view link, sorry!");
        enableReadonlyMode();
        return;
    }
    
    const id = hash[2];
    const iv = hash[3];
    const key = hash[4];

    const response = await fetch(`api/bn/${id}`);

    const responseModel = await response.json();

    if(!responseModel.success) {
        showAlert("danger", `Failed to retrieve note. Maybe it was already read?`);
        return;
    }

    const encryptedBytes = Uint8Array.from(atob(responseModel.result), c => c.charCodeAt(0));
    const ivBytes = Uint8Array.from(atob(iv), c => c.charCodeAt(0));
    const keyBytes = Uint8Array.from(atob(key), c => c.charCodeAt(0));

    const aesCbc = new aesjs.ModeOfOperation.cbc(keyBytes, ivBytes);
    const decryptedBytes = aesCbc.decrypt(encryptedBytes);

    textArea.value = aesjs.utils.utf8.fromBytes(decryptedBytes).trim();
    
    enableReadonlyMode();
}

async function onButtonClick(event) {
    const textArea = document.getElementById("textArea");
    const secretValue = textArea.value;

    if (secretValue === "") {
        showAlert("danger", "Secret can't be empty!");
        return;
    }

    hideAlert();

    const request = {
        text: secretValue
    };
    
    const response = await fetch("api/bn/create", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(request)
    });
    
    const responseModel = await response.json();
    
    if(!responseModel.success) {
        showAlert("danger", `Failed to create note. Error code: ${responseModel.error.code}. ${responseModel.error.message}`);
        return;
    }
    
    setResponseUrl(responseModel);
    enableReadonlyMode();
    
    const expirationHours = (responseModel.result.noteExpirationInMinutes / 60).toFixed(1);
    
    showAlert("success", `Your note has been created. Keep in mind that it will auto-delete itself in ${expirationHours} hours.`);
}

function showAlert(type, text) {
    const alert = document.getElementById("errorAlert");
    
    alert.className = `alert alert-${type} subtitle`;
    alert.innerText = text;
    alert.style.display = "block";
}

function hideAlert() {
    const alert = document.getElementById("errorAlert");

    alert.style.display = "none";
}

function enableReadonlyMode() {
    const generateButton = document.getElementById("createLinkBtn");
    const resetButton = document.getElementById("resetBtn");
    const textArea = document.getElementById("textArea");

    generateButton.style.display = "none";
    resetButton.style.display = "block";
    textArea.setAttribute("readonly", "");
}

// Uhh.. I stole that.
// https://stackoverflow.com/a/17259991/12030195
function adjustHeight(el){
    const newHeight = (el.scrollHeight > el.clientHeight) ? (Math.min(el.scrollHeight, 260)) : 180;
    el.style.height = `${newHeight}px`;
}

function setResponseUrl(responseModel) {
    const data = responseModel.result;
    const id = data.id;
    const iv = data.iv;
    const key = data.key;

    const pureUrl = getPureUrl();
    const newUrl = `${pureUrl}#v#${id}#${iv}#${key}`;
    
    const textArea = document.getElementById("textArea");
    textArea.value = newUrl;
}