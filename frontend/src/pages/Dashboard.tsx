import React, { useState, useEffect } from 'react';
import Layout from '../components/common/Layout';
import Card from '../components/common/Card';
import { useAppStore } from '../stores';
import { candidateApi } from '../services/candidateApi';
import { getStatusColor, CandidateStatuses } from '../types/candidate';

const DashboardPage: React.FC = () => {
  const { theme, setTheme } = useAppStore();
  const [statusTotals, setStatusTotals] = useState<Record<string, number>>({});
  const [loadingTotals, setLoadingTotals] = useState(true);

  useEffect(() => {
    const fetchStatusTotals = async () => {
      try {
        setLoadingTotals(true);
        const totals = await candidateApi.getStatusTotals();
        setStatusTotals(totals);
      } catch (error) {
        console.error('Error fetching status totals:', error);
      } finally {
        setLoadingTotals(false);
      }
    };

    fetchStatusTotals();
  }, []);

  // Toggle theme
  const handleThemeToggle = () => {
    setTheme(theme === 'light' ? 'dark' : 'light');
  };

  return (
    <Layout>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Recruitment Dashboard</h1>
        <p className="text-gray-600">Track candidate status and recruitment metrics</p>
      </div>

      {/* Status Totals Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-4 mb-8">
        {Object.values(CandidateStatuses).map((status) => {
          const count = statusTotals[status] || 0;
          const statusColorClass = getStatusColor(status);
          
          return (
            <Card key={status} className="text-center">
              {loadingTotals ? (
                <div className="animate-pulse">
                  <div className="h-8 bg-gray-200 rounded mb-2"></div>
                  <div className="h-4 bg-gray-200 rounded"></div>
                </div>
              ) : (
                <>
                  <div className="text-2xl font-bold text-gray-900 mb-1">
                    {count}
                  </div>
                  <div className={`text-sm font-medium px-2 py-1 rounded-full ${statusColorClass}`}>
                    {status}
                  </div>
                </>
              )}
            </Card>
          );
        })}
      </div>

      {/* Additional Dashboard Content */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-3">
          <h2 className="text-lg font-semibold mb-4">Quick Actions</h2>
          <p className="mb-4">Manage candidates and recruitment workflow.</p>
          
          <div className="space-x-4">
            <button
              onClick={() => window.location.href = '/candidates'}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
            >
              View All Candidates
            </button>
            
            <button
              onClick={handleThemeToggle}
              className="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700"
            >
              Toggle Theme ({theme})
            </button>
          </div>
        </Card>
      </div>
    </Layout>
  );
};

export default DashboardPage;