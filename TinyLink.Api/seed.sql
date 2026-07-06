CREATE USER backend WITH PASSWORD 'dbpassword';
GRANT CONNECT ON DATABASE tinylink_dev TO backend;
GRANT CREATE ON DATABASE tinylink_dev TO backend;
GRANT USAGE, CREATE ON SCHEMA public TO backend;

