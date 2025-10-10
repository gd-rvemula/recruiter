export interface SkillFrequency {
  text: string;
  value: number;
}

export interface SkillsApiResponse {
  skills: SkillFrequency[];
}
