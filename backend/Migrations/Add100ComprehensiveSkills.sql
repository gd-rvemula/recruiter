-- Add 100 Comprehensive Skills Migration
-- Date: September 29, 2025
-- Description: Adding 100 comprehensive skills across programming, web technologies, databases, cloud, mobile, data science, business, and security domains

-- Insert 100 new skills into the skills table
INSERT INTO skills (skill_name, created_at, updated_at) VALUES
-- Programming Languages (20 skills)
('JavaScript', NOW(), NOW()),
('TypeScript', NOW(), NOW()),
('Python', NOW(), NOW()),
('Java', NOW(), NOW()),
('React', NOW(), NOW()),
('Angular', NOW(), NOW()),
('Vue.js', NOW(), NOW()),
('Node.js', NOW(), NOW()),
('PHP', NOW(), NOW()),
('Ruby', NOW(), NOW()),
('Go', NOW(), NOW()),
('Rust', NOW(), NOW()),
('Swift', NOW(), NOW()),
('Kotlin', NOW(), NOW()),
('Scala', NOW(), NOW()),
('R', NOW(), NOW()),
('MATLAB', NOW(), NOW()),
('C++', NOW(), NOW()),
('C', NOW(), NOW()),
('Objective-C', NOW(), NOW()),

-- Web Technologies & Frameworks (15 skills)
('HTML5', NOW(), NOW()),
('CSS3', NOW(), NOW()),
('SASS/SCSS', NOW(), NOW()),
('Bootstrap', NOW(), NOW()),
('Tailwind CSS', NOW(), NOW()),
('jQuery', NOW(), NOW()),
('Express.js', NOW(), NOW()),
('Django', NOW(), NOW()),
('Flask', NOW(), NOW()),
('Ruby on Rails', NOW(), NOW()),
('Spring Boot', NOW(), NOW()),
('Laravel', NOW(), NOW()),
('ASP.NET Core', NOW(), NOW()),
('Next.js', NOW(), NOW()),
('Nuxt.js', NOW(), NOW()),

-- Database Technologies (12 skills)
('PostgreSQL', NOW(), NOW()),
('MySQL', NOW(), NOW()),
('MongoDB', NOW(), NOW()),
('Redis', NOW(), NOW()),
('Elasticsearch', NOW(), NOW()),
('Oracle Database', NOW(), NOW()),
('Microsoft SQL Server', NOW(), NOW()),
('SQLite', NOW(), NOW()),
('Cassandra', NOW(), NOW()),
('Neo4j', NOW(), NOW()),
('DynamoDB', NOW(), NOW()),
('Firebase', NOW(), NOW()),

-- Cloud & DevOps (15 skills)
('AWS', NOW(), NOW()),
('Microsoft Azure', NOW(), NOW()),
('Google Cloud Platform', NOW(), NOW()),
('Docker', NOW(), NOW()),
('Kubernetes', NOW(), NOW()),
('Jenkins', NOW(), NOW()),
('GitLab CI/CD', NOW(), NOW()),
('GitHub Actions', NOW(), NOW()),
('Terraform', NOW(), NOW()),
('Ansible', NOW(), NOW()),
('Chef', NOW(), NOW()),
('Puppet', NOW(), NOW()),
('Nagios', NOW(), NOW()),
('Prometheus', NOW(), NOW()),
('Grafana', NOW(), NOW()),

-- Mobile Development (8 skills)
('iOS Development', NOW(), NOW()),
('Android Development', NOW(), NOW()),
('React Native', NOW(), NOW()),
('Flutter', NOW(), NOW()),
('Xamarin', NOW(), NOW()),
('Ionic', NOW(), NOW()),
('Cordova/PhoneGap', NOW(), NOW()),
('Unity', NOW(), NOW()),

-- Data Science & Analytics (10 skills)
('Data Analysis', NOW(), NOW()),
('Machine Learning', NOW(), NOW()),
('Deep Learning', NOW(), NOW()),
('Natural Language Processing', NOW(), NOW()),
('Computer Vision', NOW(), NOW()),
('TensorFlow', NOW(), NOW()),
('PyTorch', NOW(), NOW()),
('Pandas', NOW(), NOW()),
('NumPy', NOW(), NOW()),
('Apache Spark', NOW(), NOW()),

-- Business & Soft Skills (10 skills)
('Project Management', NOW(), NOW()),
('Agile/Scrum', NOW(), NOW()),
('Leadership', NOW(), NOW()),
('Communication', NOW(), NOW()),
('Problem Solving', NOW(), NOW()),
('Team Management', NOW(), NOW()),
('Strategic Planning', NOW(), NOW()),
('Business Analysis', NOW(), NOW()),
('Requirements Gathering', NOW(), NOW()),
('Stakeholder Management', NOW(), NOW()),

-- Security & Testing (10 skills)
('Cybersecurity', NOW(), NOW()),
('Penetration Testing', NOW(), NOW()),
('Unit Testing', NOW(), NOW()),
('Integration Testing', NOW(), NOW()),
('Test Automation', NOW(), NOW()),
('Selenium', NOW(), NOW()),
('Jest', NOW(), NOW()),
('Cypress', NOW(), NOW()),
('Security Auditing', NOW(), NOW()),
('OWASP', NOW(), NOW())

ON CONFLICT (skill_name) DO NOTHING;

-- Show final count and all skills
SELECT COUNT(*) as total_skills FROM skills;
SELECT skill_name FROM skills ORDER BY skill_name;