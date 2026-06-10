# Project-Specific Agent Configuration

## Available Custom Agents

This project is configured to work with specialized agents that can handle different aspects of development:

### 1. **production-architect**
**Best for**: End-to-end feature delivery with production quality
- Full-stack implementation from design to deployment
- Enterprise-grade standards and best practices
- MAANG-level code quality

**How to trigger**:
- Ask to "build this production-grade"
- Request "I need MAANG-level quality"
- Say "plan and execute this feature"
- Ask to "help me deliver this properly"

### 2. **explore**
**Best for**: Understanding codebase structure and researching components
- Fast parallel investigation across multiple areas
- Codebase analysis and architecture review
- Dependency and pattern discovery

### 3. **code-review**
**Best for**: Quality assurance and identifying issues
- Analyzes code changes for bugs and vulnerabilities
- Never modifies code—only reports issues
- High signal-to-noise ratio

### 4. **task**
**Best for**: Running builds, tests, lints, and verification
- Verbose output on failure
- Brief summary on success
- Keeps main context clean

---

## Project Development Standards

### Code Organization
- **Frontend**: Well-structured component hierarchy with shared utilities
- **Backend**: Clear separation of concerns with middleware/service layers
- **Tests**: Co-located with source or in `__tests__` directories
- **Configuration**: Environment-specific configs in `.env` files

### Required Before Deployment
- ✅ All tests passing
- ✅ Linting clean (ESLint, Prettier)
- ✅ No console errors or warnings
- ✅ Performance metrics acceptable
- ✅ Accessibility tested (axe)
- ✅ Documentation updated

### Commit Message Format
```
[type](scope): Brief description

Detailed explanation of what and why, not how.
Related issues: #123, #456
```

Types: feat, fix, docs, style, refactor, test, chore

---

## Tools Available in This Environment

### Development
- **Node.js 20** with npm
- **Python 3.11** with pip
- **TypeScript** for type safety
- **Webpack**, **Vite**, **esbuild** for bundling
- **Babel** for transpilation

### Code Quality
- **ESLint** + **Prettier** for JavaScript/TypeScript
- **Stylelint** for CSS
- **Markdownlint** for documentation
- **Black**, **Flake8**, **Pylint** for Python

### Frontend
- **Tailwind CSS** for styling
- **Jest** + **React Testing Library** for testing
- **Lighthouse** for performance audits
- **axe-core** for accessibility testing

### Backend
- **Jest**, **Mocha**, **Chai** for testing
- **Prisma**, **Sequelize**, **TypeORM** for ORMs
- **Swagger/OpenAPI** for API documentation
- **Supertest** for API testing

### Architecture Analysis
- **Depcheck** for unused dependencies
- **Webpack Bundle Analyzer** for bundle insights
- **Madge** for circular dependency detection
- **Source Map Explorer** for code size analysis

---

## When to Use Each Agent

### Use production-architect when:
- Building new significant features
- Planning complex multi-service integrations
- Need to ensure production-grade quality
- Large refactoring initiatives

### Use explore when:
- Need to understand unfamiliar codebase areas
- Researching multiple components in parallel
- Analyzing project structure and dependencies
- Discovery phase before implementation

### Use code-review when:
- Reviewing pull requests before merge
- Auditing code for security/performance
- Identifying potential bugs or improvements
- Quality gate before deployment

### Use task when:
- Running tests, builds, or linting
- Verifying code quality gates pass
- Executing shell commands
- Quick verification tasks

---

## Performance Targets

### Metrics to Monitor
- **Largest Contentful Paint (LCP)**: < 2.5 seconds
- **First Input Delay (FID)**: < 100ms
- **Cumulative Layout Shift (CLS)**: < 0.1
- **Initial JS Bundle**: < 170KB (gzipped)
- **API Response Time**: < 200ms (95th percentile)

### Testing Requirements
- **Unit Test Coverage**: Target 80%+
- **Critical Paths**: 100% coverage for payment, auth, data flows
- **E2E Tests**: For user-facing features
- **Performance Tests**: Before major releases

---

## Security Checklist

Before committing, ensure:
- [ ] No secrets or API keys in code
- [ ] Input validation on all forms/APIs
- [ ] SQL injection prevention (parameterized queries)
- [ ] CORS properly configured
- [ ] Authentication/authorization checks in place
- [ ] Sensitive data encrypted
- [ ] Dependency vulnerabilities checked (`npm audit`)

---

## Quick Start Commands

```bash
# Install dependencies
npm install

# Run development server
npm run dev

# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Lint code
npm run lint

# Format code
npm run format

# Build for production
npm run build

# Run security audit
npm audit

# Analyze bundle size
npm run analyze
```

---

## Documentation Locations

- **Architecture**: See `docs/architecture.md` or `ARCHITECTURE.md`
- **API**: Check `docs/api.md` or inline JSDoc comments
- **Setup**: See `README.md` and `CONTRIBUTING.md`
- **Deployment**: See `docs/deployment.md` or `.github/workflows/`

---

## Getting Help

1. **Use `/help`** in the CLI to see all available commands
2. **Use `/skills`** to manage agent capabilities
3. **Use `/agent`** to select specific agents for a task
4. **Ask direct questions** about code or architecture

---

*Last updated: 2026*
