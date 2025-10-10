import { SkillFrequency } from '../types/skills';

const API_BASE_URL = 'http://localhost:8080/api';

export const skillsApi = {
  async getSkillsFrequency(): Promise<SkillFrequency[]> {
    const response = await fetch(`${API_BASE_URL}/candidates/skills/frequency`);
    if (!response.ok) {
      throw new Error('Failed to fetch skills frequency');
    }
    return response.json();
  }
};
