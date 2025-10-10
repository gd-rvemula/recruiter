import React, { useState, useEffect, useCallback } from 'react';
import ReactWordcloud from 'react-wordcloud';
import Layout from '../components/common/Layout';
import Card from '../components/common/Card';
import { candidateApi } from '../services/candidateApi';
import { SkillFrequency } from '../types/candidate';

// Import required CSS for tooltips
import 'tippy.js/dist/tippy.css';
import 'tippy.js/animations/scale.css';

const SkillsPage: React.FC = () => {
  const [skills, setSkills] = useState<SkillFrequency[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Simple test to see if component renders
  console.log('SkillsPage component rendering...', { skills, loading, error });

  useEffect(() => {
    const fetchSkills = async () => {
      try {
        setLoading(true);
        const skillsData = await candidateApi.getSkillsFrequency();
        setSkills(skillsData);
        setError(null);
      } catch (err) {
        console.error('Error fetching skills:', err);
        setError('Failed to load skills data');
      } finally {
        setLoading(false);
      }
    };

    fetchSkills();
  }, []);

  // Word cloud configuration
  const callbacks = {
    getWordColor: useCallback((word: SkillFrequency) => {
      // Color based on frequency - more frequent skills are darker
      if (word.value > 500) return '#1f2937'; // Dark gray for very frequent
      if (word.value > 300) return '#374151'; // Medium dark gray
      if (word.value > 100) return '#4b5563'; // Medium gray
      if (word.value > 50) return '#6b7280';  // Light gray
      return '#9ca3af'; // Very light gray for less frequent
    }, []),
    
    getWordTooltip: useCallback((word: SkillFrequency) => 
      `${word.text}: ${word.value} candidates`, []),

    onWordClick: useCallback((word: SkillFrequency) => {
      console.log('Clicked skill:', word.text);
      // TODO: Navigate to candidates page filtered by this skill
      // navigate(`/candidates?skill=${word.text}`);
    }, []),

    onWordMouseOver: useCallback((word: SkillFrequency) => {
      console.log('Hovering over:', word.text);
    }, [])
  };

  const options = {
    rotations: 1,
    rotationAngles: [0, 0] as [number, number],
    fontSizes: [16, 60] as [number, number],
    padding: 2,
    spiral: 'archimedean' as const,
    transitionDuration: 300
  };

  const size: [number, number] = [800, 500];

  if (loading) {
    return (
      <Layout>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Skills Overview</h1>
          <p className="text-gray-600">Discover the most in-demand skills in our candidate pool</p>
        </div>
        
        <Card>
          <div className="flex items-center justify-center h-96">
            <div className="animate-pulse flex space-x-4">
              <div className="rounded-full bg-gray-300 h-10 w-10"></div>
              <div className="flex-1 space-y-2 py-1">
                <div className="h-4 bg-gray-300 rounded w-3/4"></div>
                <div className="h-4 bg-gray-300 rounded w-1/2"></div>
              </div>
            </div>
          </div>
        </Card>
      </Layout>
    );
  }

  if (error) {
    return (
      <Layout>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Skills Overview</h1>
          <p className="text-gray-600">Discover the most in-demand skills in our candidate pool</p>
        </div>
        
        <Card>
          <div className="flex items-center justify-center h-96">
            <div className="text-center">
              <div className="text-red-500 text-lg mb-2">⚠️</div>
              <p className="text-gray-600">{error}</p>
              <button 
                onClick={() => window.location.reload()} 
                className="mt-4 px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Try Again
              </button>
            </div>
          </div>
        </Card>
      </Layout>
    );
  }

  if (skills.length === 0) {
    return (
      <Layout>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-gray-900">Skills Overview</h1>
          <p className="text-gray-600">Discover the most in-demand skills in our candidate pool</p>
        </div>
        
        <Card>
          <div className="flex items-center justify-center h-96">
            <div className="text-center">
              <p className="text-gray-600">No skills data available</p>
            </div>
          </div>
        </Card>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Skills Overview</h1>
        <p className="text-gray-600">
          Discover the most in-demand skills in our candidate pool. 
          Hover over skills to see candidate counts.
        </p>
      </div>

      {/* Skills Statistics */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
        <Card className="text-center">
          <div className="text-2xl font-bold text-blue-600 mb-1">
            {skills.length}
          </div>
          <div className="text-sm text-gray-600">Total Skills</div>
        </Card>
        
        <Card className="text-center">
          <div className="text-2xl font-bold text-green-600 mb-1">
            {skills.reduce((sum, skill) => sum + skill.value, 0)}
          </div>
          <div className="text-sm text-gray-600">Total Skill Instances</div>
        </Card>
        
        <Card className="text-center">
          <div className="text-2xl font-bold text-purple-600 mb-1">
            {skills[0]?.text || 'N/A'}
          </div>
          <div className="text-sm text-gray-600">Top Skill</div>
        </Card>
        
        <Card className="text-center">
          <div className="text-2xl font-bold text-orange-600 mb-1">
            {skills[0]?.value || 0}
          </div>
          <div className="text-sm text-gray-600">Top Skill Count</div>
        </Card>
      </div>

      {/* Word Cloud */}
      <Card>
        <div className="p-6">
          <div className="flex justify-center">
            <div style={{ width: size[0], height: size[1] }}>
              {skills.length > 0 ? (
                <ReactWordcloud
                  words={skills}
                  callbacks={callbacks}
                  options={options}
                  size={size}
                />
              ) : (
                <div className="flex items-center justify-center h-full">
                  <p className="text-gray-500">Loading word cloud...</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </Card>

      {/* Top Skills Table */}
      <Card className="mt-6">
        <div className="p-6">
          <h3 className="text-lg font-semibold mb-4">
            Top Skills ({Math.min(skills.length, 20)} of {skills.length})
          </h3>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Rank
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Skill
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Candidates
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Percentage
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {skills.slice(0, 20).map((skill, index) => {
                  const totalCandidates = skills.reduce((sum, s) => sum + s.value, 0);
                  const percentage = ((skill.value / totalCandidates) * 100).toFixed(1);
                  
                  return (
                    <tr key={skill.text} className="hover:bg-gray-50">
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        #{index + 1}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {skill.text}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {skill.value}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {percentage}%
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </Card>
    </Layout>
  );
};

export default SkillsPage;