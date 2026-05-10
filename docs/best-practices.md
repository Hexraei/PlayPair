# Development Best Practices & Standards

This document outlines comprehensive best practices for full-stack development to ensure high code quality, maintainability, and performance.

## 1. Frontend Development

### Component Architecture

**Atomic Design Pattern:**
```
components/
├── atoms/          # Button, Input, Label
├── molecules/      # FormGroup, Card, Modal
├── organisms/      # Header, Footer, Form
├── templates/      # PageLayout
└── pages/          # ActualPages
```

**Component Best Practices:**
- Single Responsibility: Each component should do one thing
- Composition: Combine components instead of creating complex ones
- Prop Validation: Use TypeScript interfaces or PropTypes
- Avoid Prop Drilling: Use Context API or state management for deep trees
- Memoization: Use React.memo for expensive components

### State Management

**Rules:**
- **Normalized State**: Store entities flat, use IDs for relationships
- **Single Source of Truth**: Don't duplicate data
- **Derivable State**: Don't store computed values (use selectors)
- **Async State**: Handle loading, error, and success states explicitly

Example structure:
```javascript
{
  user: { byId: { "1": {...} }, allIds: ["1"] },
  posts: { byId: { "1": {...} }, allIds: ["1"] },
  ui: { isLoading: false, error: null, selectedUserId: "1" }
}
```

### Performance Optimization

**Code Splitting:**
```javascript
const HeavyComponent = React.lazy(() => import('./Heavy'));
<Suspense fallback={<LoadingSpinner />}>
  <HeavyComponent />
</Suspense>
```

**Memoization:**
```javascript
// Components
const MyComponent = React.memo(({ data, onUpdate }) => {...});

// Callbacks
const handleClick = useCallback(() => {
  // implementation
}, [dependency]);

// Values
const memoizedValue = useMemo(() => expensiveComputation(), [dep]);
```

**Image Optimization:**
- Use WebP with fallbacks
- Lazy load below-the-fold images
- Optimize image dimensions
- Use responsive images with srcset

### Accessibility (A11y)

**Critical Requirements:**
- Semantic HTML: Use `<button>`, `<nav>`, `<main>`, `<article>`
- ARIA Labels: Add descriptions for screen readers
- Keyboard Navigation: All interactive elements must be keyboard accessible
- Color Contrast: Minimum 4.5:1 for text, 3:1 for large text
- Focus Management: Clear focus indicators, logical tab order

Example:
```jsx
<button
  aria-label="Close menu"
  onClick={handleClose}
  className="focus:ring-2 focus:ring-offset-2"
>
  ✕
</button>
```

---

## 2. Backend Development

### API Design

**RESTful Conventions:**
- GET: Retrieve resources (safe, idempotent)
- POST: Create new resources
- PUT: Replace entire resource
- PATCH: Partial updates
- DELETE: Remove resources

**Response Format:**
```javascript
{
  success: true,
  data: { /* resource */ },
  meta: { pagination: { page: 1, limit: 20, total: 100 } }
}
```

**Error Format:**
```javascript
{
  success: false,
  error: {
    code: "VALIDATION_ERROR",
    message: "User-friendly message",
    details: [{ field: "email", message: "Invalid format" }]
  }
}
```

### Authentication & Authorization

**JWT Implementation:**
```javascript
const token = jwt.sign(
  { userId, role },
  process.env.JWT_SECRET,
  { expiresIn: '24h' }
);
```

**Authorization Middleware:**
```javascript
const authorize = (requiredRoles) => (req, res, next) => {
  if (!requiredRoles.includes(req.user.role)) {
    return res.status(403).json({ error: 'Forbidden' });
  }
  next();
};
```

### Database Optimization

**Indexing Strategy:**
- Index frequently queried columns
- Composite indexes for multi-column queries
- Avoid over-indexing (slows writes)

**Query Optimization:**
- Use pagination for large datasets
- Select only needed fields
- Join optimization (avoid N+1 queries)
- Use database query explanations

**Example:**
```javascript
// Good: Only fetch needed fields with pagination
User.find({}, { name: 1, email: 1 })
  .limit(20)
  .skip((page - 1) * 20)
  .lean(); // Skip Mongoose wrapping for read-only data

// Avoid N+1 queries with population/joins
const users = await User.find().populate('posts');
```

### Security

**Input Validation:**
```javascript
const { body, validationResult } = require('express-validator');

router.post('/users', [
  body('email').isEmail(),
  body('password').isLength({ min: 8 }),
  body('name').trim().notEmpty()
], (req, res) => {
  const errors = validationResult(req);
  if (!errors.isEmpty()) {
    return res.status(400).json({ errors: errors.array() });
  }
});
```

**Password Hashing:**
```javascript
const bcrypt = require('bcrypt');
const hashedPassword = await bcrypt.hash(password, 10);
const isValid = await bcrypt.compare(inputPassword, hashedPassword);
```

**Environment Variables:**
```bash
# .env (never commit!)
DATABASE_URL=postgresql://user:pass@localhost/db
JWT_SECRET=your-secret-key
API_KEY=external-service-key
NODE_ENV=production
```

---

## 3. Testing Strategy

### Unit Tests
```javascript
describe('UserService', () => {
  it('should create user with valid data', () => {
    const user = UserService.create({ name: 'John', email: 'john@example.com' });
    expect(user.name).toBe('John');
    expect(user.id).toBeDefined();
  });

  it('should throw error with invalid email', () => {
    expect(() => {
      UserService.create({ name: 'John', email: 'invalid' });
    }).toThrow('Invalid email');
  });
});
```

### Integration Tests
```javascript
describe('User API', () => {
  it('should create user via POST /users', async () => {
    const res = await request(app)
      .post('/users')
      .send({ name: 'John', email: 'john@example.com' });
    
    expect(res.status).toBe(201);
    expect(res.body.data.id).toBeDefined();
  });
});
```

### E2E Tests
```javascript
describe('User Registration Flow', () => {
  it('should register new user and login', () => {
    cy.visit('/register');
    cy.get('input[name="email"]').type('john@example.com');
    cy.get('input[name="password"]').type('SecurePass123');
    cy.get('button[type="submit"]').click();
    cy.url().should('include', '/dashboard');
  });
});
```

---

## 4. Code Quality Standards

### TypeScript Configuration

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "strictFunctionTypes": true,
    "noImplicitThis": true,
    "alwaysStrict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

### ESLint & Prettier

**ESLint Config:**
```json
{
  "extends": ["eslint:recommended", "next/core-web-vitals"],
  "rules": {
    "no-console": ["warn", { "allow": ["warn", "error"] }],
    "no-debugger": "error",
    "prefer-const": "error",
    "no-var": "error"
  }
}
```

### Documentation

**JSDoc Standards:**
```javascript
/**
 * Fetches user by ID from database
 * @param {string} userId - The user's unique identifier
 * @returns {Promise<User>} User object with sanitized data
 * @throws {Error} If user not found or database error
 * @example
 * const user = await getUser('123');
 */
async function getUser(userId) {
  // implementation
}
```

---

## 5. Git Workflow

### Commit Messages

**Format:**
```
[type](scope): description

Body explaining what and why (not how)

Related issues: #123
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Formatting
- `refactor`: Code restructuring
- `test`: Test additions/fixes
- `chore`: Dependency updates, build config

**Example:**
```
feat(auth): implement JWT refresh token rotation

- Add refresh token with 7-day expiration
- Implement rotation on each use
- Store refresh tokens in httpOnly cookies
- Add token revocation on logout

Fixes #456
```

### Branch Naming

```
feature/add-user-authentication
bugfix/fix-memory-leak
docs/update-api-documentation
chore/update-dependencies
```

---

## 6. Performance Checklist

### Frontend
- [ ] Bundle size < 170KB (gzipped)
- [ ] LCP < 2.5s
- [ ] FID < 100ms
- [ ] CLS < 0.1
- [ ] Images optimized with WebP
- [ ] Code splitting for routes
- [ ] Memoization for expensive renders

### Backend
- [ ] API response time < 200ms (p95)
- [ ] Database queries optimized
- [ ] Caching implemented (Redis/HTTP)
- [ ] Pagination for large datasets
- [ ] Connection pooling configured
- [ ] Gzip compression enabled

---

## 7. Deployment Readiness

Before deploying:
- ✅ All tests passing
- ✅ No console errors/warnings
- ✅ Linting clean
- ✅ Security audit passed
- ✅ Environment variables documented
- ✅ Database migrations tested
- ✅ Rollback plan in place
- ✅ Monitoring alerts configured

---

## 8. Common Pitfalls to Avoid

❌ **Don't:**
- Mutate state directly
- Ignore error handling
- Log sensitive data
- Leave console.logs in production
- Skip unit tests
- Use `any` types in TypeScript
- Hardcode values in code
- Ignore accessibility
- Over-fetch data
- Not validate user input

✅ **Do:**
- Create immutable copies
- Handle errors explicitly
- Use environment variables
- Remove debug statements
- Write tests alongside code
- Use proper types
- Use configuration files
- Test with screen readers
- Implement pagination
- Validate on frontend AND backend

---

## Quick Reference

| Task | Command |
|------|---------|
| Run tests | `npm test` |
| Check coverage | `npm run test:coverage` |
| Lint code | `npm run lint` |
| Format code | `npm run format` |
| Build | `npm run build` |
| Analyze bundle | `npm run analyze` |
| Run audit | `npm audit` |
| Check types | `tsc --noEmit` |

---

*Keep code simple, tests comprehensive, and users happy.*
