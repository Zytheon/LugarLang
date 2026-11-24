
import type { ContentBundle, ContentVersion } from "../types/content";

const API_BASE_URL = "https://lugarlang-api.onrender.com";

export async function getContent(): Promise<ContentBundle> {
    const response = await fetch(`${API_BASE_URL}/api/content`);

    if (!response.ok) {
        throw new Error(
            `Failed to retrieve LugarLang content: HTTP ${response.status}`
        );
    }

    return response.json() as Promise<ContentBundle>;
}

export async function getContentVersion(): Promise<ContentVersion> {
    const response = await fetch(`${API_BASE_URL}/api/content/version`);

    if (!response.ok) {
        throw new Error(
            `Failed to retrieve LugarLang content version: HTTP ${response.status}`
        );
    }

    return response.json() as Promise<ContentVersion>;
}
