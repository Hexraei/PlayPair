# Copilot Agent Instructions for Full-Stack Development Excellence

## Core Principles
- **Context Awareness**: Always understand the full codebase structure, dependencies, and architectural patterns before making changes
- **User-Centric Design**: Consider end-user experience and accessibility in every decision
- **Best Practices First**: Apply industry-standard patterns and conventions from MAANG-level companies

---

## 1. Code Context & Architecture

### Understanding Context
- **Analyze the entire codebase** before proposing changes—understand module relationships, data flow, and dependencies
- **Map architectural patterns**: Identify if using MVC, microservices, monolith, or other patterns
- **Review existing conventions**: Match coding style, naming patterns, and folder structure
- **Consider constraints**: Database limitations, performance requirements, security needs

### Architectural Thinking
- **Separation of Concerns**: Keep business logic, UI, and data layers separate
- **Modularity**: Create reusable, testable components and functions
- **Scalability**: Design with growth in mind—use middleware, caching, queuing where appropriate
- **Maintainability**: Write code that's easy for others to understand and extend

---

## 2. Planning & Design

### Before Implementation
1. **Break down the task** into logical phases
2. **Identify dependencies** and execution order
3. **Consider edge cases** and error scenarios
4. **Document assumptions** and constraints
5. **Propose the approach** before coding

### Architectural Design
- **Domain-Driven Design (DDD)**: Use bounded contexts for complex systems
- **SOLID Principles**:
  - Single Responsibility
  - Open/Closed for extension
  - Liskov Substitution
  - Interface Segregation
  - Dependency Inversion
- **Design Patterns**: Factory, Strategy, Observer, Middleware as appropriate

---

## 3. UI/UX Design Sense

### User Experience First
- **Usability**: Follow Nielsen's 10 Usability Heuristics
- **Accessibility (A11y)**: 
  - WCAG 2.1 AA standards minimum
  - Semantic HTML, ARIA labels, keyboard navigation
  - Color contrast ratios (4.5:1 for text)
  - Mobile-first responsive design
- **Performance**:
  - Lazy loading for images and components
  - Code splitting and bundle optimization
  - LCP < 2.5s, FID < 100ms, CLS < 0.1

### Visual Design Principles
- **Consistency**: Use design tokens, component libraries (Tailwind, Material Design)
- **White Space**: Proper spacing and padding for readability
- **Typography**: Readable font sizes, line heights (1.5-1.8), proper contrast
- **Color Psychology**: Intentional color choices for brand and emotion
- **Visual Hierarchy**: Size, color, position guide attention
- **Feedback**: Loading states, error messages, success confirmations

---

## 4. Interaction & Error Handling

### User Interactions
- **Feedback loops**: Users should always know what's happening
- **Loading states**: Show skeleton screens or spinners during async operations
- **Error handling**: Clear, actionable error messages (not technical jargon)
- **Validation**: Real-time feedback for forms, helpful hints
- **Transitions**: Smooth animations that don't distract

### Error Handling Excellence
```
DO:
- "Please enter a valid email address (example@domain.com)"
- Highlight problematic fields
- Suggest solutions
- Log errors for debugging

DON'T:
- "Error 400: Bad Request"
- Generic "Something went wrong"
- Silent failures
- Multiple error messages overwhelming users
```

### State Management
- **Clear data flow**: Unidirectional where possible (Redux, Zustand, Context)
- **Single source of truth**: Avoid data duplication
- **Optimistic updates**: Update UI before server confirmation when safe
- **Rollback capability**: Handle failed operations gracefully

---

## 5. Full-Stack Development Excellence

### Backend Best Practices
- **API Design**:
  - RESTful conventions or GraphQL where appropriate
  - Versioning strategy (headers, URLs)
  - Consistent response formats
  - Rate limiting and throttling
  - Proper HTTP status codes
- **Security**:
  - Input validation and sanitization
  - SQL injection prevention (parameterized queries)
  - CORS configuration
  - JWT/OAuth implementation
  - Environment variable management
- **Performance**:
  - Database indexing and query optimization
  - Caching strategies (Redis, HTTP caching)
  - Async operations and job queues
  - Connection pooling

### Frontend Best Practices
- **Component Architecture**:
  - Atomic design or similar methodology
  - Single Responsibility Principle for components
  - Props validation (TypeScript or PropTypes)
  - Composition over inheritance
- **State Management**:
  - Normalized state shape
  - Derived state vs. stored state
  - Side effects management (useEffect patterns)
- **Performance Optimization**:
  - Memoization (React.memo, useMemo, useCallback)
  - Code splitting and lazy loading
  - Tree shaking unused code
  - Image optimization

### Testing Strategy
- **Unit Tests**: Pure functions, utilities, component logic
- **Integration Tests**: Component interactions, data flow
- **E2E Tests**: Critical user journeys
- **Performance Tests**: Load testing, bundle size analysis
- **Test Coverage**: Target 80%+ for critical paths

---

## 6. Code Quality Standards

### Standards to Enforce
- **TypeScript**: Strong typing for large projects
- **Linting**: ESLint with strict config
- **Formatting**: Prettier for consistency
- **Git Workflow**: Meaningful commits, good PR descriptions
- **Documentation**: JSDoc comments for public APIs, README files
- **Naming Conventions**:
  - camelCase for variables/functions
  - PascalCase for classes/components
  - UPPER_SNAKE_CASE for constants
  - Descriptive, searchable names (avoid single letters except loops)

### Code Review Checklist
- [ ] Does it solve the problem described?
- [ ] Does it break existing tests?
- [ ] Are there performance implications?
- [ ] Is error handling comprehensive?
- [ ] Is it accessible (A11y)?
- [ ] Does it follow project conventions?
- [ ] Is it documented and tested?
- [ ] Could someone understand this in 6 months?

---

## 7. Development Workflow

### When Starting a Task
1. **Understand the requirement** fully
2. **Map the scope** and identify dependencies
3. **Create a plan** with steps and checkpoints
4. **Implement incrementally**, testing as you go
5. **Document assumptions** and decisions

### Tools and Commands to Use
- **Linting**: `npm run lint` or `eslint .`
- **Formatting**: `npm run format` or `prettier --write .`
- **Testing**: `npm test` or `pytest`
- **Type Checking**: `tsc --noEmit`
- **Build**: `npm run build` or equivalent
- **Performance**: `lighthouse`, `webpack-bundle-analyzer`, `source-map-explorer`

---

## 8. Security Considerations

- **Data Protection**:
  - Encrypt sensitive data at rest and in transit
  - Hash passwords with bcrypt or similar
  - Never log sensitive information
- **Authentication & Authorization**:
  - Implement proper session/token management
  - Use established libraries (Passport, NextAuth)
  - Principle of least privilege
- **Dependency Management**:
  - Regular security audits (`npm audit`, `pip check`)
  - Update dependencies regularly
  - Use lockfiles (package-lock.json)
- **Input Validation**:
  - Validate all user inputs server-side
  - Sanitize for XSS prevention
  - CSRF tokens for state-changing operations

---

## 9. Performance Optimization

### Metrics to Monitor
- **Core Web Vitals**: LCP, FID, CLS
- **Bundle Size**: Keep initial JS < 170KB
- **Time to Interactive (TTI)**: < 3.8s
- **API Response Time**: < 200ms for 95th percentile

### Optimization Techniques
- **Frontend**: Lazy loading, code splitting, image optimization, memoization
- **Backend**: Query optimization, caching, indexing, asynchronous processing
- **Network**: CDN usage, compression, HTTP/2, prefetching
- **Monitoring**: Use tools like New Relic, DataDog, or similar

---

## 10. Communication & Documentation

### What to Document
- **Architecture decisions**: ADRs (Architecture Decision Records)
- **Complex algorithms**: Explain the "why"
- **API endpoints**: Clear examples and error responses
- **Setup instructions**: For new developers
- **Deployment process**: Step-by-step guides

### Comments Should Explain
- **Why**, not what (code shows what)
- Complex business logic
- Workarounds and their reasons
- Links to relevant issues/PRs

---

## Summary: Agent Success Criteria

When completing a task, ensure:
✅ **Context**: Full understanding of codebase and requirements  
✅ **Planning**: Clear approach documented before implementation  
✅ **Design**: Thoughtful UI/UX with accessibility  
✅ **Quality**: Tests pass, linting clean, no console errors  
✅ **Performance**: Optimized with metrics verified  
✅ **Security**: Validated inputs, proper auth, no sensitive data leaks  
✅ **Documentation**: Code is self-explanatory, complex logic commented  
✅ **User Experience**: Error handling, loading states, clear feedback  

When unsure, err on the side of:
- More tests than less
- More documentation than less
- More user feedback than less
- More optimization than less
