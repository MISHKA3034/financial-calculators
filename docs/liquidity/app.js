import { dotnet } from "../_framework/dotnet.js";

const mode = new URL(import.meta.url).pathname.split("/").at(-2);
let runtime;
async function loadCalculator() {
    const { getAssemblyExports, getConfig } = await dotnet.create();
    const exports = await getAssemblyExports(getConfig().mainAssemblyName);
    return exports.BrowserApi;
}

const form = document.querySelector("form");
const output = document.querySelector("#output");
const submit = form.querySelector("button");
const format = new Intl.NumberFormat("ru", { maximumFractionDigits: 6 });

document.querySelector("#example").addEventListener("click", () => {
    for (const field of form.querySelectorAll("input")) field.value = field.dataset.example;
    form.requestSubmit();
});

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (submit.disabled) return;
    const data = {};
    for (const [name, value] of new FormData(form)) {
        const normalized = value.trim().replace(",", ".");
        if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(normalized) || !Number.isFinite(Number(normalized))) {
            output.textContent = "Проверьте формат чисел. Допускается запятая или точка.";
            return;
        }
        data[name] = Number(normalized);
    }
    submit.disabled = true;
    output.textContent = "Выполняется расчёт…";
    try {
        const calculator = await (runtime ??= loadCalculator());
        const payload = calculator.Calculate(mode, JSON.stringify(data));
        const result = JSON.parse(payload);
        if (result.error) {
            output.textContent = result.error;
            return;
        }

        const score = document.createElement("strong");
        score.textContent = format.format(result.score);
        const message = document.createElement("p");
        message.textContent = result.message;
        output.replaceChildren(score, message);
        
    } catch {
        output.textContent = "Не удалось загрузить модуль расчёта. Обновите страницу.";
    } finally {
        submit.disabled = false;
    }
});


